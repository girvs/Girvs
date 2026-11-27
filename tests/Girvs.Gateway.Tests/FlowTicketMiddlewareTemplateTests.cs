using System.Net;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Gateway.Tests;

public class FlowTicketMiddlewareTemplateTests
{
    [Theory]
    [InlineData(false, 42)]
    [InlineData(true, 42)]
    [InlineData(false, 99)]
    [InlineData(true, 99)]
    public async Task 模板首步签发票据_后续步骤以同一业务标识持票完成(bool parameterized, int confirmOrderId)
    {
        const string first = "/orders/{orderId}/prepare";
        const string second = "/orders/{orderId}/confirm";
        const string businessId = "42";
        const string preparePath = "/service/orders/42/prepare";
        var confirmPath = $"/service/orders/{confirmOrderId}/confirm";
        var options = new FlowProtectionOptions
        {
            Enabled = true,
            Flows =
            [
                parameterized
                    ? FlowDefinitionRegistryTemplateTests.Parameterized("orders", first, second)
                    : FlowDefinitionRegistryVariantsTests.CreateSingleVariant("orders", first, second),
            ],
        };
        var forwardedPaths = new List<string?>();
        var service = new FlowTicketService(new InMemoryFlowStateStore(), options.LockTtlSeconds);
        var transform = new FlowResponseTransform(options, service);
        var middleware = new FlowTicketMiddleware(
            async context =>
            {
                forwardedPaths.Add(context.Request.Path.Value);
                using var downstream = new HttpResponseMessage(HttpStatusCode.OK);
                await transform.ProcessAsync(new ResponseTransformContext { HttpContext = context, ProxyResponse = downstream });
            },
            options,
            new FlowDefinitionRegistry(options),
            new DownstreamPathResolver(),
            service,
            NullLogger<FlowTicketMiddleware>.Instance
        );

        var start = CreateContext(preparePath, businessId);
        if (parameterized)
        {
            start.Request.Headers["X-Mode"] = "AB";
        }

        await middleware.InvokeAsync(start);
        Assert.Equal(StatusCodes.Status200OK, start.Response.StatusCode);
        var ticket = Assert.Single(start.Response.Headers[FlowProtectionHeaders.Ticket]);
        Assert.False(string.IsNullOrWhiteSpace(ticket));
        Assert.Equal("1", Assert.Single(start.Response.Headers[FlowProtectionHeaders.NextIndex]));

        var finish = CreateContext(confirmPath, businessId);
        finish.Request.Headers[FlowProtectionHeaders.Ticket] = ticket;
        await middleware.InvokeAsync(finish);

        Assert.Equal(StatusCodes.Status200OK, finish.Response.StatusCode);
        Assert.Equal("true", Assert.Single(finish.Response.Headers[FlowProtectionHeaders.Completed]));
        Assert.False(finish.Response.Headers.ContainsKey(FlowProtectionHeaders.Ticket));
        Assert.Equal([preparePath, confirmPath], forwardedPaths);
    }

    private static DefaultHttpContext CreateContext(string path, string businessId)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.Request.Headers[FlowProtectionHeaders.BusinessId] = businessId;
        context.Features.Set<IReverseProxyFeature>(new ReverseProxyFeature
        {
            Route = new RouteModel(
                new RouteConfig
                {
                    RouteId = "service",
                    ClusterId = "downstream",
                    Match = new RouteMatch { Path = "/service/{**catch-all}" },
                    Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = "/service" }],
                },
                cluster: null,
                transformer: HttpTransformer.Default
            ),
        });
        return context;
    }
}
