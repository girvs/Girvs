using Girvs.Aspire.Gateway.FlowProtection.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Girvs.Aspire.Gateway.FlowProtection;

public sealed class FlowResponseTransform(
    FlowProtectionOptions options,
    FlowTicketService ticketService
) : ITransformProvider
{
    private const string BusinessIdResponseHeader = "X-Flow-Business-Id";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddResponseTransform(ProcessAsync);
    }

    internal async ValueTask ProcessAsync(ResponseTransformContext context)
    {
        if (!options.Enabled)
        {
            return;
        }

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
                await ticketService.DeleteAsync(attempt, context.CancellationToken);
            }
            else
            {
                await ticketService.ReleaseAsync(attempt, context.CancellationToken);
            }
        }
        catch
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.SuppressResponseBody = true;
            throw;
        }
        finally
        {
            context.HttpContext.Response.Headers.Remove(BusinessIdResponseHeader);
        }
    }

    private static string ReadBusinessId(ResponseTransformContext context)
    {
        if (context.ProxyResponse?.Headers.TryGetValues(BusinessIdResponseHeader, out var values) == true)
        {
            return values.FirstOrDefault() ?? string.Empty;
        }

        if (context.ProxyResponse?.Content?.Headers.TryGetValues(BusinessIdResponseHeader, out values) == true)
        {
            return values.FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    private void WriteSuccessHeaders(
        HttpContext context,
        FlowAttemptContext attempt,
        FlowStoreResult result
    )
    {
        context.Response.Headers.CacheControl = "no-store";
        if (result.Completed)
        {
            context.Response.Headers["X-Flow-Completed"] = "true";
            return;
        }

        context.Response.Headers[options.TicketHeaderName] = attempt.Ticket;
        if (result.NextIndex.HasValue)
        {
            context.Response.Headers["X-Flow-Next-Index"] = result.NextIndex.Value.ToString();
        }
    }
}
