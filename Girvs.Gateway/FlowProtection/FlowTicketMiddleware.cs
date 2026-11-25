using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Girvs.Gateway.FlowProtection;

public sealed class FlowTicketMiddleware(
    RequestDelegate next,
    FlowProtectionOptions options,
    FlowDefinitionRegistry registry,
    DownstreamPathResolver pathResolver,
    FlowTicketService ticketService,
    ILogger<FlowTicketMiddleware> logger
)
{
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
            await WriteConfigurationErrorAsync(context);
            return;
        }

        if (!registry.TryGetEndpoint(context.Request.Method, downstreamPath.Value, out var endpoint))
        {
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

    private Task<FlowAttemptContext> ReserveAsync(
        HttpContext context,
        FlowEndpointMatch endpoint,
        CancellationToken cancellationToken
    )
    {
        if (endpoint.LinearMatch is { } linearStep)
        {
            return ReserveLinearAsync(context, linearStep);
        }

        if (!string.IsNullOrWhiteSpace(context.Request.Headers[options.TicketHeaderName].ToString()))
        {
            return ReserveParameterizedNextAsync(context, endpoint, cancellationToken);
        }

        var variants = endpoint.Flow.Variants;
        if (ReadSelector(context, endpoint.Flow.ParameterHeaderName, out var selectorValue) != SelectorState.Present
            || variants is null
            || !variants.TryGetValue(selectorValue, out var variant))
        {
            throw new FlowProtectionException(FlowProtectionErrors.SelectorInvalid);
        }

        if (!endpoint.TryMatchVariant(variant.Key, out var stepMatch) || !stepMatch.IsFirstStep)
        {
            throw new FlowProtectionException(FlowProtectionErrors.NotFound);
        }

        var businessId = ReadBusinessId(context);
        return ticketService.CreateFirstStepAttemptAsync(businessId, stepMatch, cancellationToken);
    }

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

        var businessId = ReadBusinessId(context);
        if (businessId.Length == 0)
        {
            throw new FlowProtectionException(FlowProtectionErrors.BusinessMismatch);
        }

        string? selectorVariantKey = null;
        switch (ReadSelector(context, endpoint.Flow.ParameterHeaderName, out var selectorValue))
        {
            case SelectorState.Invalid:
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

    private string ReadBusinessId(HttpContext context)
    {
        // 与存量线性流程保持一致：同名多值头取首个值，并去除首尾空白。
        return context.Request.Headers[options.BusinessIdHeaderName].FirstOrDefault()?.Trim() ?? string.Empty;
    }

    private static SelectorState ReadSelector(HttpContext context, string headerName, out string value)
    {
        var values = context.Request.Headers[headerName];
        value = string.Empty;
        if (values.Count == 0)
        {
            return SelectorState.Absent;
        }

        if (values.Count > 1)
        {
            return SelectorState.Invalid;
        }

        value = values.ToString().Trim();
        return value.Length == 0 ? SelectorState.Invalid : SelectorState.Present;
    }

    private enum SelectorState
    {
        Absent,
        Invalid,
        Present,
    }

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
