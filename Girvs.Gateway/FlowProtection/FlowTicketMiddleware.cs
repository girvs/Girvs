using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Girvs.Gateway.FlowProtection;

public sealed class FlowTicketMiddleware(
    RequestDelegate next,
    FlowProtectionOptions options,
    FlowDefinitionRegistry registry,
    DownstreamPathResolver pathResolver,
    FlowTicketService ticketService
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

        if (!registry.TryGetStep(context.Request.Method, downstreamPath.Value, out var step))
        {
            await next(context);
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
            await next(context);
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
        var businessId = context.Request.Headers[options.BusinessIdHeaderName].FirstOrDefault() ?? string.Empty;
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
