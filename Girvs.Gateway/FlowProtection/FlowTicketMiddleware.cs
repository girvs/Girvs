using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Gateway.FlowProtection;

public sealed class FlowTicketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FlowProtectionOptions _options;
    private readonly FlowDefinitionRegistry _registry;
    private readonly DownstreamPathResolver _pathResolver;
    private readonly FlowTicketService _ticketService;

    /// <summary>
    /// DI 激活入口：未启用流程防护时不注册相关服务，此时不解析依赖，请求直接放行。
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public FlowTicketMiddleware(
        RequestDelegate next,
        FlowProtectionOptions options,
        IServiceProvider serviceProvider
    )
        : this(
            next,
            options,
            options.Enabled ? serviceProvider.GetRequiredService<FlowDefinitionRegistry>() : null,
            options.Enabled ? serviceProvider.GetRequiredService<DownstreamPathResolver>() : null,
            options.Enabled ? serviceProvider.GetRequiredService<FlowTicketService>() : null
        ) { }

    public FlowTicketMiddleware(
        RequestDelegate next,
        FlowProtectionOptions options,
        FlowDefinitionRegistry registry,
        DownstreamPathResolver pathResolver,
        FlowTicketService ticketService
    )
    {
        _next = next;
        _options = options;
        _registry = registry;
        _pathResolver = pathResolver;
        _ticketService = ticketService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var downstreamPath = _pathResolver.Resolve(context);
        if (downstreamPath is null)
        {
            await WriteConfigurationErrorAsync(context);
            return;
        }

        if (!_registry.TryGetStep(context.Request.Method, downstreamPath.Value, out var step))
        {
            await _next(context);
            return;
        }

        FlowAttemptContext attempt;
        try
        {
            attempt = await ReserveAsync(context, step);
        }
        catch (FlowProtectionException ex)
        {
            await WriteProblemAsync(context, ex.Error);
            return;
        }

        context.Items[FlowAttemptContext.ItemsKey] = attempt;
        try
        {
            await _next(context);
        }
        finally
        {
            if (!attempt.IsFinalized)
            {
                await FinalizeUncommittedAttemptAsync(attempt, context.RequestAborted);
            }
        }
    }

    private async Task<FlowAttemptContext> ReserveAsync(HttpContext context, FlowStepMatch step)
    {
        var businessId = context.Request.Headers[_options.BusinessIdHeaderName].FirstOrDefault() ?? string.Empty;
        if (step.IsFirstStep)
        {
            return await _ticketService.CreateFirstStepAttemptAsync(businessId, step, context.RequestAborted);
        }

        var ticket = context.Request.Headers[_options.TicketHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ticket))
        {
            throw new FlowProtectionException(FlowProtectionErrors.NotFound);
        }

        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new FlowProtectionException(FlowProtectionErrors.BusinessMismatch);
        }

        return await _ticketService.ReserveNextStepAttemptAsync(ticket, businessId, step, context.RequestAborted);
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
                await _ticketService.DeleteAsync(attempt, cancellationToken);
            }
            else
            {
                await _ticketService.ReleaseAsync(attempt, cancellationToken);
            }
        }
        catch (FlowProtectionException)
        {
            attempt.IsFinalized = true;
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, FlowProtectionError error)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Type = $"https://errors.example.com/flow/{error.Slug}",
            Title = error.Title,
            Status = StatusCodes.Status403Forbidden,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem);
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
