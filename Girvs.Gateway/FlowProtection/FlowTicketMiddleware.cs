using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程票据中间件：在反向代理之前判定请求属于“开启新流程”还是“推进既有流程”，预留顺位并加处理锁。
/// </summary>
/// <param name="next">中间件链中的下一个处理器（即反向代理）。</param>
/// <param name="options">流程防护配置，提供启用开关、票据头名、业务标识头名与前置参数头名来源。</param>
/// <param name="registry">流程定义注册表，提供按方法与下游路径反查接口归属的能力。</param>
/// <param name="pathResolver">下游路径还原器，把客户端路径转换为配置中书写形式的下游路径。</param>
/// <param name="ticketService">票据服务，执行预留、提交、释放与删除。</param>
/// <param name="logger">日志记录器，用于清理失败等非致命异常。</param>
/// <remarks>
/// <para>
/// 判别规则：请求是否携带票据头是唯一分界。未携带一律按新流程首步处理（参数化流程还需合法前置参数）；
/// 携带则一律按票据已锁定的流程组合推进，并由存储原子校验归属、业务标识与顺位。
/// </para>
/// <para>
/// 每个被命中的请求都会先预留（加锁），再交给下游；响应阶段的结算由
/// <see cref="FlowResponseTransform"/> 完成，若响应阶段未能结算，本中间件在请求结束时做兜底清理。
/// </para>
/// </remarks>
public sealed class FlowTicketMiddleware(
    RequestDelegate next,
    FlowProtectionOptions options,
    FlowDefinitionRegistry registry,
    DownstreamPathResolver pathResolver,
    FlowTicketService ticketService,
    ILogger<FlowTicketMiddleware> logger
)
{
    /// <summary>
    /// 处理一次请求：判定归属、预留顺位，然后放行到下游，并在结束时兜底清理未结算的尝试。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <remarks>
    /// <para>
    /// 提前返回的三种情况：流程防护未启用（直接放行）、无法还原下游路径（配置错误 500）、
    /// 接口不在任何流程定义内（非受保护请求，直接放行）。
    /// </para>
    /// <para>
    /// 预留失败抛出的 <see cref="FlowProtectionException"/> 会被转换为 403 问题详情，
    /// 此时请求不会到达下游，也不会留下处理锁。
    /// </para>
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.Enabled)
        {
            await next(context);
            return;
        }

        var downstreamPath = pathResolver.Resolve(context);
        if (downstreamPath is null)
        {
            // 路径无法唯一还原说明路由转换配置与流程防护前提不符，必须显式报错而不是静默放行。
            await WriteConfigurationErrorAsync(context);
            return;
        }

        if (!registry.TryGetEndpoint(context.Request.Method, downstreamPath.Value, out var endpoint))
        {
            // 非受保护接口：不做任何票据处理，直接透传给反向代理。
            await next(context);
            return;
        }

        FlowAttemptContext attempt;
        try
        {
            attempt = await ReserveAsync(context, endpoint, context.RequestAborted);
        }
        catch (FlowProtectionException ex)
        {
            await WriteProblemAsync(context, ex.Error);
            return;
        }

        // 供响应阶段与兜底清理复用同一实例，保证预留与结算严格成对。
        context.Items[FlowAttemptContext.ItemsKey] = attempt;
        try
        {
            await next(context);
        }
        finally
        {
            if (!attempt.IsFinalized)
            {
                // 使用独立的短超时：RequestAborted 在客户端断开时已取消，会让清理直接失败；
                // 清理异常绝不能覆盖已经写出的响应。
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await FinalizeUncommittedAttemptAsync(attempt, timeout.Token);
            }
        }
    }

    /// <summary>
    /// 按接口归属形态选择预留策略：线性流程按其固定顺位，参数化流程按是否携带票据分别处理。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="endpoint">命中的接口归属信息。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>预留成功的尝试状态对象。</returns>
    /// <exception cref="FlowProtectionException">
    /// 前置参数缺失或非法、接口不是所选组合的首步、票据缺失、业务标识缺失等。
    /// </exception>
    private Task<FlowAttemptContext> ReserveAsync(
        HttpContext context,
        FlowEndpointMatch endpoint,
        CancellationToken cancellationToken
    )
    {
        if (endpoint.LinearMatch is { } linearStep)
        {
            // 线性流程没有前置参数概念，直接按配置顺位处理。
            return ReserveLinearAsync(context, linearStep);
        }

        if (!string.IsNullOrWhiteSpace(context.Request.Headers[options.TicketHeaderName].ToString()))
        {
            // 持票即按已锁定组合推进：同一接口可能既是某组合首步、又是另一组合中间步，只有票据能消除歧义。
            return ReserveParameterizedNextAsync(context, endpoint, cancellationToken);
        }

        // 无票：必须用前置参数选定一条组合，且当前接口必须是该组合第 1 步。
        var variants = endpoint.Flow.Variants;
        if (ReadSelector(context, endpoint.Flow.ParameterHeaderName, out var selectorValue) != SelectorState.Present
            || variants is null
            || !variants.TryGetValue(selectorValue, out var variant))
        {
            // 无票且参数缺失/空白/同名多值/未命中任何已配置取值，一律拒绝，不提供默认组合。
            throw new FlowProtectionException(FlowProtectionErrors.SelectorInvalid);
        }

        if (!endpoint.TryMatchVariant(variant.Key, out var stepMatch) || !stepMatch.IsFirstStep)
        {
            // 参数合法但当前接口不是该组合首步：按“票据不存在”处理，避免泄露组合内容。
            throw new FlowProtectionException(FlowProtectionErrors.NotFound);
        }

        var businessId = ReadBusinessId(context);
        return ticketService.CreateFirstStepAttemptAsync(businessId, stepMatch, cancellationToken);
    }

    /// <summary>
    /// 按票据已锁定的流程组合推进参数化流程的下一步。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="endpoint">命中的接口归属信息，含该接口在各组合中的候选顺位。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>预留成功的尝试状态对象。</returns>
    /// <exception cref="FlowProtectionException">
    /// 票据为空、业务标识缺失、前置参数非法或与锁定组合不一致、或顺位不允许。
    /// </exception>
    /// <remarks>
    /// 推进阶段不强制重传前置参数；若携带则必须与票据锁定组合一致。
    /// 具体的组合一致性判定交由存储的原子脚本完成，本方法只负责把候选信息整理好传下去。
    /// </remarks>
    private Task<FlowAttemptContext> ReserveParameterizedNextAsync(
        HttpContext context,
        FlowEndpointMatch endpoint,
        CancellationToken cancellationToken
    )
    {
        var ticket = context.Request.Headers[options.TicketHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ticket))
        {
            throw new FlowProtectionException(FlowProtectionErrors.NotFound);
        }

        // 推进阶段必须提供业务标识，否则无法确认调用方与票据绑定的是同一业务对象。
        var businessId = ReadBusinessId(context);
        if (businessId.Length == 0)
        {
            throw new FlowProtectionException(FlowProtectionErrors.BusinessMismatch);
        }

        string? selectorVariantKey = null;
        switch (ReadSelector(context, endpoint.Flow.ParameterHeaderName, out var selectorValue))
        {
            case SelectorState.Invalid:
                // 参数头存在但为空或同名多值：语义无法确定，直接按不一致拒绝。
                throw new FlowProtectionException(FlowProtectionErrors.SelectorMismatch);
            case SelectorState.Present:
                // 命中已配置组合时统一传规范化组合键；未命中则传原值，交由原子脚本在完成态校验后判 SELECTOR_MISMATCH。
                selectorVariantKey = endpoint.Flow.Variants is { } variants
                    && variants.TryGetValue(selectorValue, out var selectorVariant)
                        ? selectorVariant.Key
                        : selectorValue;
                break;
        }

        return ticketService.ReserveNextStepAttemptAsync(
            ticket,
            businessId,
            endpoint,
            selectorVariantKey,
            cancellationToken
        );
    }

    /// <summary>
    /// 读取业务标识请求头。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>去除首尾空白后的业务标识；未提供时为空字符串。</returns>
    private string ReadBusinessId(HttpContext context)
    {
        // 与存量线性流程保持一致：同名多值头取首个值，并去除首尾空白。
        return context.Request.Headers[options.BusinessIdHeaderName].FirstOrDefault()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 读取并判定前置参数请求头的状态。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="headerName">前置参数所在的请求头名。</param>
    /// <param name="value">存在唯一有效值时返回去除首尾空白后的取值；否则为空字符串。</param>
    /// <returns>请求头的三态判定结果。</returns>
    /// <remarks>
    /// 严格区分“未携带”“携带但非法”“携带且唯一有效”：前两者在开启新流程时应被拒绝，
    /// 而“携带但非法”在推进流程时对应组合不一致，因此不能简单合并为布尔判断。
    /// </remarks>
    private static SelectorState ReadSelector(HttpContext context, string headerName, out string value)
    {
        var values = context.Request.Headers[headerName];
        value = string.Empty;
        if (values.Count == 0)
        {
            return SelectorState.Absent;
        }

        // 同名多值无法确定调用方意图，一律视为非法，绝不取首个值猜测。
        if (values.Count > 1)
        {
            return SelectorState.Invalid;
        }

        value = values.ToString().Trim();
        return value.Length == 0 ? SelectorState.Invalid : SelectorState.Present;
    }

    /// <summary>
    /// 前置参数请求头的三态判定结果。
    /// </summary>
    private enum SelectorState
    {
        /// <summary>请求未携带该头。</summary>
        Absent,

        /// <summary>携带了该头但取值为空白或同名多值。</summary>
        Invalid,

        /// <summary>携带了该头且存在唯一、非空白的取值。</summary>
        Present,
    }

    /// <summary>
    /// 按线性流程的固定顺位预留：首步创建票据，其余步骤凭票据与业务标识推进。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="step">命中的线性步骤匹配。</param>
    /// <returns>预留成功的尝试状态对象。</returns>
    /// <exception cref="FlowProtectionException">票据缺失、业务标识缺失或顺位不允许。</exception>
    private async Task<FlowAttemptContext> ReserveLinearAsync(HttpContext context, FlowStepMatch step)
    {
        var businessId = ReadBusinessId(context);
        if (step.IsFirstStep)
        {
            return await ticketService.CreateFirstStepAttemptAsync(businessId, step, context.RequestAborted);
        }

        var ticket = context.Request.Headers[options.TicketHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ticket))
        {
            throw new FlowProtectionException(FlowProtectionErrors.NotFound);
        }

        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new FlowProtectionException(FlowProtectionErrors.BusinessMismatch);
        }

        return await ticketService.ReserveNextStepAttemptAsync(ticket, businessId, step, context.RequestAborted);
    }

    /// <summary>
    /// 兜底清理：响应阶段未结算的尝试在此收尾，首步失败删票、其余步骤释放锁。
    /// </summary>
    /// <param name="attempt">未结算的尝试状态对象。</param>
    /// <param name="cancellationToken">独立于客户端连接的取消标记。</param>
    /// <remarks>
    /// 清理失败只记录警告而不上抛：处理锁的租约会自然到期释放，
    /// 而此处若抛异常会掩盖下游已经产生的真实响应或原始异常。
    /// </remarks>
    private async Task FinalizeUncommittedAttemptAsync(
        FlowAttemptContext attempt,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (attempt.IsFirstStep)
            {
                await ticketService.DeleteAsync(attempt, cancellationToken);
            }
            else
            {
                await ticketService.ReleaseAsync(attempt, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // 清理失败只记录日志：租约会自然到期释放，不能让异常从 finally 逃逸。
            logger.LogWarning(ex, "流程票据 {Ticket} 清理失败，将在租约到期后自动释放", attempt.Ticket);
        }
    }

    /// <summary>
    /// 写出 403 问题详情响应。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="error">触发拒绝的流程防护错误。</param>
    /// <remarks>
    /// 响应体遵循 RFC 7807：<c>type</c> 由错误的短标识拼接，扩展字段携带稳定错误码与请求追踪标识，
    /// 便于客户端分支处理与线上排障。
    /// </remarks>
    private static async Task WriteProblemAsync(HttpContext context, FlowProtectionError error)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        var problem = new ProblemDetails
        {
            Type = $"https://errors.example.com/flow/{error.Slug}",
            Title = error.Title,
            Status = StatusCodes.Status403Forbidden,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    /// <summary>
    /// 写出 500 配置错误响应，用于下游路径无法还原的情况。
    /// </summary>
    /// <param name="context">当前请求上下文。</param>
    /// <remarks>
    /// 该错误表示网关自身的路由转换配置与流程防护前提不一致（无法唯一确定去除的前缀），
    /// 属于部署问题而非客户端问题，因此返回 500 并给出固定错误码，提醒运维检查路由配置。
    /// </remarks>
    private static async Task WriteConfigurationErrorAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Type = "https://errors.example.com/flow/configuration-error",
            Title = "流程保护路由配置错误",
            Status = StatusCodes.Status500InternalServerError,
        };
        problem.Extensions["code"] = "FLOW_CONFIGURATION_ERROR";
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
