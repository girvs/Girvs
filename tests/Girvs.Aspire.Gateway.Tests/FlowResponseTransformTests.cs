using System.Net;
using Girvs.Aspire.Gateway.FlowProtection;
using Girvs.Aspire.Gateway.FlowProtection.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Aspire.Gateway.Tests;

public class FlowResponseTransformTests
{
    [Fact]
    public async Task 下游2xx_Commit并写入下一步Header()
    {
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, completed: false));
        var httpContext = CreateHttpContext(firstStep: true);
        var transform = CreateTransform(store);
        var proxyResponse = new HttpResponseMessage(HttpStatusCode.OK);

        await transform.ProcessAsync(new ResponseTransformContext
        {
            HttpContext = httpContext,
            ProxyResponse = proxyResponse,
        });

        Assert.NotNull(store.LastCommitRequest);
        Assert.Equal("ticket", httpContext.Response.Headers["X-Flow-Ticket"]);
        Assert.Equal("1", httpContext.Response.Headers["X-Flow-Next-Index"]);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task 下游非2xx_Release且不推进Header()
    {
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, completed: false));
        var httpContext = CreateHttpContext(firstStep: false);
        var transform = CreateTransform(store);
        var proxyResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        await transform.ProcessAsync(new ResponseTransformContext
        {
            HttpContext = httpContext,
            ProxyResponse = proxyResponse,
        });

        Assert.NotNull(store.LastReleaseRequest);
        Assert.False(httpContext.Response.Headers.ContainsKey("X-Flow-Next-Index"));
    }

    [Fact]
    public async Task 最后一步2xx_写入完成Header()
    {
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(3, completed: true));
        var httpContext = CreateHttpContext(firstStep: false, lastStep: true);
        var transform = CreateTransform(store);

        await transform.ProcessAsync(new ResponseTransformContext
        {
            HttpContext = httpContext,
            ProxyResponse = new HttpResponseMessage(HttpStatusCode.NoContent),
        });

        Assert.Equal("true", httpContext.Response.Headers["X-Flow-Completed"]);
    }

    [Fact]
    public async Task 下游BusinessIdHeader_提交但不暴露给客户端()
    {
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, completed: false));
        var httpContext = CreateHttpContext(firstStep: true);
        var transform = CreateTransform(store);
        var proxyResponse = new HttpResponseMessage(HttpStatusCode.OK);
        proxyResponse.Headers.Add("X-Flow-Business-Id", "biz-from-service");
        httpContext.Response.Headers["X-Flow-Business-Id"] = "biz-from-service";

        await transform.ProcessAsync(new ResponseTransformContext
        {
            HttpContext = httpContext,
            ProxyResponse = proxyResponse,
        });

        Assert.Equal("biz-from-service", store.LastCommitRequest.BusinessIdFromResponse);
        Assert.False(httpContext.Response.Headers.ContainsKey("X-Flow-Business-Id"));
    }

    private static FlowResponseTransform CreateTransform(RecordingFlowStateStore store)
    {
        return new FlowResponseTransform(
            new FlowProtectionOptions { Enabled = true },
            new FlowTicketService(store)
        );
    }

    private static HttpContext CreateHttpContext(bool firstStep, bool lastStep = false)
    {
        var context = new DefaultHttpContext();
        var steps = new[]
        {
            new FlowStepDefinition("POST", "/api/a"),
            new FlowStepDefinition("POST", "/api/b"),
            new FlowStepDefinition("POST", "/api/c"),
        };
        var stepIndex = lastStep ? 2 : 0;
        context.Items[FlowAttemptContext.ItemsKey] = new FlowAttemptContext
        {
            Ticket = "ticket",
            AttemptId = "attempt",
            BusinessId = "biz",
            Step = new FlowStepMatch(new FlowDefinition("flow-1", 300, steps), steps[stepIndex], stepIndex),
            IsFirstStep = firstStep,
        };
        return context;
    }

    private sealed class RecordingFlowStateStore(FlowStoreResult result) : IFlowStateStore
    {
        public FlowCommitRequest LastCommitRequest { get; private set; }

        public FlowReleaseRequest LastReleaseRequest { get; private set; }

        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Task.FromResult(result);

        public Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);

        public Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken)
        {
            LastCommitRequest = request;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
        {
            LastReleaseRequest = request;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }
}
