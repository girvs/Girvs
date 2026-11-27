using System.Net;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Yarp.ReverseProxy.Transforms;
using static Girvs.Gateway.Tests.FlowDefinitionRegistryVariantsTests;

namespace Girvs.Gateway.Tests;

public class FlowResponseTransformTests
{
    [Fact]
    public async Task 下游2xx_Commit并写入下一步Header()
    {
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey));
        var httpContext = CreateHttpContext("/api/a");
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
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey));
        var httpContext = CreateHttpContext("/api/b");
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
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(3, true, DefaultVariantKey));
        var httpContext = CreateHttpContext("/api/c");
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
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey));
        var httpContext = CreateHttpContext("/api/a");
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

    [Fact]
    public async Task 提交票据异常_设置503抑制响应体并剥离业务标识()
    {
        var failure = new InvalidOperationException("状态存储不可用");
        var store = new RecordingFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey), failure);
        var httpContext = CreateHttpContext("/api/a");
        httpContext.Response.Headers[FlowProtectionHeaders.BusinessIdFromResponse] = "biz-from-service";
        using var proxyResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("downstream") };
        var context = new ResponseTransformContext { HttpContext = httpContext, ProxyResponse = proxyResponse };

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateTransform(store).ProcessAsync(context).AsTask());

        Assert.Same(failure, thrown);
        Assert.NotNull(store.LastCommitRequest);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.True(context.SuppressResponseBody);
        Assert.False(httpContext.Response.Headers.ContainsKey(FlowProtectionHeaders.BusinessIdFromResponse));
        Assert.False(httpContext.Response.Headers.ContainsKey(FlowProtectionHeaders.Ticket));
        Assert.False(httpContext.Response.Headers.ContainsKey(FlowProtectionHeaders.NextIndex));
        Assert.False(httpContext.Response.Headers.ContainsKey(FlowProtectionHeaders.Completed));
    }

    private static FlowResponseTransform CreateTransform(RecordingFlowStateStore store)
    {
        return new FlowResponseTransform(
            new FlowProtectionOptions { Enabled = true },
            new FlowTicketService(store)
        );
    }

    private static HttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        var registry = new FlowDefinitionRegistry(new FlowProtectionOptions
        {
            Flows = [CreateSingleVariant("flow-1", "/api/a", "/api/b", "/api/c")],
        });
        Assert.True(registry.TryGetEndpoint("POST", path, out var endpoint));
        Assert.True(endpoint.TryMatchVariant(DefaultVariantKey, out var step));
        context.Items[FlowAttemptContext.ItemsKey] = new FlowAttemptContext
        {
            Ticket = "ticket",
            AttemptId = "attempt",
            BusinessId = "biz",
            Step = step,
            IsFirstStep = step.IsFirstStep,
        };
        return context;
    }

    private sealed class RecordingFlowStateStore(FlowStoreResult result, Exception? commitException = null) : IFlowStateStore
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
            return commitException is null ? Task.FromResult(result) : Task.FromException<FlowStoreResult>(commitException);
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
