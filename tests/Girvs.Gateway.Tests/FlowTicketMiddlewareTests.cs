using System.Text.Json;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

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
            FlowStoreResult.Failed(FlowErrorCode.FlowStepNotAllowed)
        ).InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("FLOW_STEP_NOT_ALLOWED", body);
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
    }

    private static FlowTicketMiddleware CreateMiddleware(
        RequestDelegate next,
        FlowStoreResult result = null
    )
    {
        var options = Options();
        return new FlowTicketMiddleware(
            next,
            options,
            new FlowDefinitionRegistry(options),
            new DownstreamPathResolver(),
            new FlowTicketService(new FakeFlowStateStore(result ?? FlowStoreResult.Success(1, completed: false)))
        );
    }

    private static FlowProtectionOptions Options()
    {
        return new FlowProtectionOptions
        {
            Enabled = true,
            Flows =
            [
                new FlowDefinitionOptions
                {
                    FlowId = "flow-1",
                    Steps =
                    [
                        new FlowStepOptions { Method = "POST", Path = "/api/user1" },
                        new FlowStepOptions { Method = "POST", Path = "/api/role1" },
                        new FlowStepOptions { Method = "POST", Path = "/api/abc1" },
                    ],
                },
            ],
        };
    }

    private static HttpContext CreateContext(string method, string path)
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
                        Transforms =
                        [
                            new Dictionary<string, string> { ["PathRemovePrefix"] = "order" },
                        ],
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
        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Task.FromResult(result);

        public Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
            => Task.FromResult(result);

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
