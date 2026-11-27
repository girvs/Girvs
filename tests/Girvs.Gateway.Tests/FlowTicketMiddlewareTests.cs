using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using static Girvs.Gateway.Tests.FlowDefinitionRegistryVariantsTests;

namespace Girvs.Gateway.Tests;

public class FlowTicketMiddlewareTests
{
    [Fact]
    public async Task 第二步无Ticket_返回403且不调用下游()
    {
        var context = CreateContext("POST", "/order/api/role1");
        var called = false;

        await CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("FLOW_NOT_FOUND", body);
    }

    [Fact]
    public async Task 跳过步骤_返回403且不调用下游()
    {
        var context = CreateContext("POST", "/order/api/abc1");
        context.Request.Headers["X-Flow-Ticket"] = "ticket";
        context.Request.Headers["X-Business-Id"] = "biz-1";
        var called = false;

        await CreateMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            new FakeFlowStateStore(FlowStoreResult.Failed(FlowErrorCode.FlowStepNotAllowed))
        ).InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("FLOW_STEP_NOT_ALLOWED", body);
    }

    [Theory]
    [InlineData(FlowErrorCode.FlowStepNotAllowed, "FLOW_STEP_NOT_ALLOWED")]
    [InlineData(FlowErrorCode.FlowCompleted, "FLOW_COMPLETED")]
    public async Task 免前置参数单组合持票调用首步_返回403且不重开票据(FlowErrorCode error, string code)
    {
        var context = CreateContext("POST", "/order/api/user1");
        context.Request.Headers[FlowProtectionHeaders.Ticket] = "ticket";
        context.Request.Headers[FlowProtectionHeaders.BusinessId] = "biz-1";
        var store = new FakeFlowStateStore(FlowStoreResult.Failed(error));

        await CreateMiddleware(_ => throw new InvalidOperationException("请求不应转发"), store).InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains(code, await ReadBodyAsync(context));
        Assert.False(context.Response.Headers.ContainsKey(FlowProtectionHeaders.Ticket));
        Assert.Equal(0, store.CreateCount);
        var reserve = Assert.IsType<FlowReserveRequest>(store.LastReserveRequest);
        Assert.Equal("ticket", reserve.Ticket);
        Assert.Empty(reserve.SelectorVariantKey);
        Assert.Equal(new FlowVariantStepIndex(DefaultVariantKey, 0), Assert.Single(reserve.CandidateVariantSteps!));
    }

    [Fact]
    public async Task 路由缺少去前缀转换_返回500配置错误且不调用下游()
    {
        var context = CreateContext("POST", "/order/api/user1", removePrefix: false);
        var store = new FakeFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey));

        await CreateMiddleware(_ => throw new InvalidOperationException("请求不应转发"), store).InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("FLOW_CONFIGURATION_ERROR", await ReadBodyAsync(context));
        Assert.Equal(0, store.CreateCount);
        Assert.Null(store.LastReserveRequest);
    }

    [Fact]
    public async Task 第一步Reserve成功_调用下游并保存Attempt上下文()
    {
        var context = CreateContext("POST", "/order/api/user1");
        context.Request.Headers["X-Business-Id"] = "biz-1";

        await CreateMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        var attempt = Assert.IsType<FlowAttemptContext>(context.Items[FlowAttemptContext.ItemsKey]);
        Assert.True(attempt.IsFirstStep);
        Assert.Equal("biz-1", attempt.BusinessId);
        Assert.Equal(DefaultVariantKey, attempt.Step.VariantKey);
    }

    private static FlowTicketMiddleware CreateMiddleware(
        RequestDelegate next,
        FakeFlowStateStore? store = null
    )
    {
        var options = Options();
        return new FlowTicketMiddleware(
            next,
            options,
            new FlowDefinitionRegistry(options),
            new DownstreamPathResolver(),
            new FlowTicketService(store ?? new FakeFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey))),
            NullLogger<FlowTicketMiddleware>.Instance
        );
    }

    private static FlowProtectionOptions Options()
    {
        return new FlowProtectionOptions
        {
            Enabled = true,
            Flows = [CreateSingleVariant("flow-1", "/api/user1", "/api/role1", "/api/abc1")],
        };
    }

    private static HttpContext CreateContext(string method, string path, bool removePrefix = true)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Features.Set<IReverseProxyFeature>(
            new ReverseProxyFeature
            {
                Route = new RouteModel(
                    new RouteConfig
                    {
                        RouteId = "order",
                        ClusterId = "order",
                        Match = new RouteMatch { Path = "/order/{**catch-all}" },
                        Transforms = removePrefix ? [new Dictionary<string, string> { ["PathRemovePrefix"] = "order" }] : [],
                    },
                    cluster: null,
                    transformer: HttpTransformer.Default
                ),
            }
        );
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class FakeFlowStateStore(FlowStoreResult result) : IFlowStateStore
    {
        public int CreateCount { get; private set; }

        public FlowReserveRequest? LastReserveRequest { get; private set; }

        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        )
        {
            CreateCount++;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
        {
            LastReserveRequest = request;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);

        public Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);

        public Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class ReverseProxyFeature : IReverseProxyFeature
    {
        public RouteModel Route { get; set; }

        public ClusterModel Cluster { get; set; }

        public IReadOnlyList<DestinationState> AllDestinations { get; set; }

        public IReadOnlyList<DestinationState> AvailableDestinations { get; set; }

        public DestinationState ProxiedDestination { get; set; }
    }
}
