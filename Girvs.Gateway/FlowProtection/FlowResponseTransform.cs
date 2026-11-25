using Girvs.Gateway.FlowProtection.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 反向代理响应阶段的流程票据结算器：按下游响应结果提交、释放或删除票据，并向客户端回写续用响应头。
/// </summary>
/// <param name="options">流程防护配置，提供启用开关、票据头名与完成态保留时长。</param>
/// <param name="ticketService">票据服务，负责把结算动作转成状态存储的原子操作。</param>
/// <remarks>
/// <para>
/// 结算规则：
/// 下游返回 2xx 时提交当前步骤（推进顺位，最后一步转入完成态）；
/// 非 2xx 且本次是首步时删除整张票据（撤销本次流程）；
/// 非 2xx 且本次是中间步时仅释放处理锁，使该步可被重试。
/// </para>
/// <para>
/// 只有请求阶段成功预留过票据（<see cref="FlowAttemptContext.ItemsKey"/> 存在）的请求才会进入结算，
/// 未被流程防护命中的普通请求直接跳过，不产生任何额外开销。
/// </para>
/// </remarks>
public sealed class FlowResponseTransform(
    FlowProtectionOptions options,
    FlowTicketService ticketService
) : ITransformProvider
{
    /// <summary>
    /// 路由级校验钩子。本转换不依赖具体路由元数据，无需校验，保留空实现以满足接口契约。
    /// </summary>
    /// <param name="context">路由校验上下文（未使用）。</param>
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    /// <summary>
    /// 集群级校验钩子。本转换不依赖具体集群配置，无需校验，保留空实现以满足接口契约。
    /// </summary>
    /// <param name="context">集群校验上下文（未使用）。</param>
    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    /// <summary>
    /// 向 YARP 注册响应转换，使每个代理响应都经过 <see cref="ProcessAsync"/> 结算。
    /// </summary>
    /// <param name="context">转换构建上下文。</param>
    public void Apply(TransformBuilderContext context)
    {
        context.AddResponseTransform(ProcessAsync);
    }

    /// <summary>
    /// 对单个代理响应执行票据结算并写回响应头。
    /// </summary>
    /// <param name="context">响应转换上下文，提供代理响应、原始 Http 上下文与取消标记。</param>
    /// <remarks>
    /// <para>
    /// 结算异常一律转换为 503 并抑制响应体：此时下游已经返回内容，但票据状态未能落定，
    /// 继续把下游响应交给客户端会让客户端误以为步骤成功并可继续推进。
    /// </para>
    /// <para>
    /// 无论结算成功与否，都会在结束前剥离下游回传的业务标识响应头，
    /// 保证该头永不外泄给客户端。
    /// </para>
    /// </remarks>
    internal async ValueTask ProcessAsync(ResponseTransformContext context)
    {
        if (!options.Enabled)
        {
            return;
        }

        // 未在请求阶段预留票据的请求（未被流程防护命中，或请求阶段已拒绝）无需结算。
        if (context.HttpContext.Items[FlowAttemptContext.ItemsKey] is not FlowAttemptContext attempt)
        {
            return;
        }

        var statusCode = (int?)context.ProxyResponse?.StatusCode;
        var success = statusCode is >= 200 and <= 299;
        try
        {
            if (success)
            {
                // 成功：提交当前步骤；最后一步会在存储侧把票据转为完成态并使用完成态时长。
                var result = await ticketService.CommitAsync(
                    attempt,
                    ReadBusinessId(context),
                    options.CompletedTtlSeconds,
                    context.CancellationToken
                );
                WriteSuccessHeaders(context.HttpContext, attempt, result);
            }
            else if (attempt.IsFirstStep)
            {
                // 首步失败：本次流程无法继续，删除整张票据，避免留下永远停在第 0 步的悬挂状态。
                await ticketService.DeleteAsync(attempt, context.CancellationToken);
            }
            else
            {
                // 中间步失败：票据仍然有效，只释放处理锁，允许调用方修正后重试同一步。
                await ticketService.ReleaseAsync(attempt, context.CancellationToken);
            }
        }
        catch
        {
            // 结算失败属于网关侧故障，既不回放下游响应体，也不允许客户端据此推进流程。
            context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.SuppressResponseBody = true;
            throw;
        }
        finally
        {
            // 业务标识仅用于网关内部写入，必须先于响应完成剥离，防止泄露。
            context.HttpContext.Response.Headers.Remove(FlowProtectionHeaders.BusinessIdFromResponse);
        }
    }

    /// <summary>
    /// 从下游响应中读取其回传的业务标识。
    /// </summary>
    /// <param name="context">响应转换上下文。</param>
    /// <returns>业务标识的首个取值；未回传时为空字符串。</returns>
    /// <remarks>
    /// 下游可能把该头写在响应头或内容头上（取决于是否流式返回），故两处都检查；
    /// 同名多值时取首个，与请求侧读取业务标识的策略保持一致。
    /// </remarks>
    private static string ReadBusinessId(ResponseTransformContext context)
    {
        if (context.ProxyResponse?.Headers.TryGetValues(FlowProtectionHeaders.BusinessIdFromResponse, out var values) == true)
        {
            return values.FirstOrDefault() ?? string.Empty;
        }

        if (context.ProxyResponse?.Content?.Headers.TryGetValues(FlowProtectionHeaders.BusinessIdFromResponse, out values) == true)
        {
            return values.FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// 向客户端回写成功结算后的续用响应头。
    /// </summary>
    /// <param name="context">要写出响应头的 Http 上下文。</param>
    /// <param name="attempt">本次尝试的状态对象，提供票据值。</param>
    /// <param name="result">提交结果，提供完成标记与下一个顺位。</param>
    /// <remarks>
    /// 已完成时只写完成标记，不再回传票据与顺位——此时票据在保留期内只用于拒绝重复调用。
    /// 所有成功响应都强制 <c>no-store</c>，避免中间缓存把一次性票据响应复用给其它调用方。
    /// </remarks>
    private void WriteSuccessHeaders(
        HttpContext context,
        FlowAttemptContext attempt,
        FlowStoreResult result
    )
    {
        context.Response.Headers.CacheControl = "no-store";
        if (result.Completed)
        {
            context.Response.Headers[FlowProtectionHeaders.Completed] = "true";
            return;
        }

        // 未完成：回传票据供客户端续用，并给出下一个允许顺位。
        context.Response.Headers[options.TicketHeaderName] = attempt.Ticket;
        if (result.NextIndex.HasValue)
        {
            context.Response.Headers[FlowProtectionHeaders.NextIndex] = result.NextIndex.Value.ToString();
        }
    }
}
