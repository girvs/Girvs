using Girvs.Gateway.FlowProtection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Girvs.Gateway.Tests;

public class DownstreamPathResolverTests
{
    [Fact]
    public void 入口路径含服务名前缀_还原下游路径()
    {
        var context = CreateContext("/order/api/pay1", "order");

        Assert.Equal("/api/pay1", new DownstreamPathResolver().Resolve(context)?.Value);
    }

    [Fact]
    public void 无PathRemovePrefix_返回Null()
    {
        var context = CreateContext("/order/api/pay1");

        Assert.Null(new DownstreamPathResolver().Resolve(context));
    }

    [Fact]
    public void 多个PathRemovePrefix_返回Null()
    {
        var context = CreateContext("/order/api/pay1", "order", "api");

        Assert.Null(new DownstreamPathResolver().Resolve(context));
    }

    [Fact]
    public void 入口路径不包含前缀_返回Null()
    {
        var context = CreateContext("/other/api/pay1", "order");

        Assert.Null(new DownstreamPathResolver().Resolve(context));
    }

    private static HttpContext CreateContext(string path, params string[] prefixes)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Features.Set<IReverseProxyFeature>(
            new ReverseProxyFeature
            {
                Route = new RouteModel(
                    new RouteConfig
                    {
                        RouteId = "order",
                        ClusterId = "order",
                        Match = new RouteMatch { Path = "/order/{**catch-all}" },
                        Transforms = prefixes
                            .Select(x => new Dictionary<string, string> { ["PathRemovePrefix"] = x })
                            .ToArray(),
                    },
                    cluster: null,
                    transformer: HttpTransformer.Default
                ),
            }
        );
        return context;
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
