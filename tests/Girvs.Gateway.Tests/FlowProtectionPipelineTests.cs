using System.Net;
using System.Text.Json;
using Girvs.Gateway.FlowProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Gateway.Tests;

public class FlowProtectionPipelineTests
{
    [Theory]
    [InlineData("/a", " aBe ", "ABE")]
    [InlineData("/d", " dE ", "DE")]
    public async Task 无票合法参数访问组合首步_转发并签发锁定组合的票据(string path, string selector, string variant)
    {
        var pipeline = new Pipeline();

        var context = await pipeline.SendAsync(path, selector, businessId: " biz-client ");

        var state = AssertCommittedFirstStep(pipeline, context);
        Assert.Equal("variants", state.FlowId);
        Assert.Equal(variant, state.Variant);
        Assert.Equal("biz-client", state.BusinessId);
    }

    [Fact]
    public async Task 下游首步返回业务标识_绑定票据并剥离响应头()
    {
        var pipeline = new Pipeline(HttpStatusCode.Created, "biz-from-service");

        var context = await pipeline.SendAsync("/a", "ABE");

        var state = AssertCommittedFirstStep(pipeline, context, HttpStatusCode.Created);
        Assert.Equal("biz-from-service", state.BusinessId);
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Business-Id"));
    }

    [Fact]
    public async Task 下游首步返回非2xx_删除已预留票据且不回传票据头()
    {
        var pipeline = new Pipeline(HttpStatusCode.BadRequest);

        var context = await pipeline.SendAsync("/d", "DE");

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(1, pipeline.ForwardCount);
        Assert.Equal("DE", pipeline.ForwardedTicket?.Variant);
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Processing, pipeline.ForwardedTicket?.Status);
        Assert.Null(pipeline.Store.GetTicket(pipeline.ForwardedAttempt.Ticket));
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Ticket"));
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Next-Index"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("unknown")]
    public async Task 无票缺少合法前置参数_返回SelectorInvalid且不转发(string? selector)
    {
        var pipeline = new Pipeline();

        var context = await pipeline.SendAsync("/a", selector);

        await AssertRejectedAsync(pipeline, context, "FLOW_SELECTOR_INVALID", "selector-invalid");
    }

    [Fact]
    public async Task 同名前置参数头多值_返回SelectorInvalid且不转发()
    {
        var pipeline = new Pipeline();

        var context = await pipeline.SendAsync("/a", new StringValues(["ABE", "ABE"]));

        await AssertRejectedAsync(pipeline, context, "FLOW_SELECTOR_INVALID", "selector-invalid");
    }

    [Theory]
    [InlineData("/d", "ADE")]
    [InlineData("/e", "ADE")]
    [InlineData("/d", "ABE")]
    public async Task 无票合法参数请求非组合首步_返回NotFound且不转发(string path, string selector)
    {
        var pipeline = new Pipeline();

        var context = await pipeline.SendAsync(path, selector);

        await AssertRejectedAsync(pipeline, context, "FLOW_NOT_FOUND", "not-found");
    }

    public static TheoryData<string, string[]> CompleteFlows => new()
    {
        { "ABE", ["/a", "/b", "/e"] },
        { "ACE", ["/a", "/c", "/e"] },
        { "ADE", ["/a", "/d", "/e"] },
        { "DE", ["/d", "/e"] },
    };

    [Theory]
    [MemberData(nameof(CompleteFlows))]
    public async Task 多组合完整推进_终步标记完成并拒绝重放(string variant, string[] paths)
    {
        var pipeline = new Pipeline();

        var start = await pipeline.SendAsync(paths[0], selector: variant, businessId: "biz");
        var startState = AssertCommittedFirstStep(pipeline, start);
        Assert.Equal(variant, startState.Variant);
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        for (var index = 1; index < paths.Length - 1; index++)
        {
            var before = pipeline.ForwardCount;
            var step = await pipeline.SendAsync(paths[index], ticket: ticket, businessId: "biz");

            Assert.Equal(before + 1, pipeline.ForwardCount);
            AssertAdvanced(pipeline, step, variant, index + 1);
        }

        var last = await pipeline.SendAsync(paths[^1], ticket: ticket, businessId: "biz");
        AssertCompleted(pipeline, last, variant, paths.Length);

        var beforeReplay = pipeline.ForwardCount;
        var replay = await pipeline.SendAsync(paths[^1], ticket: ticket, businessId: "biz");
        await AssertRejectedAsync(pipeline, replay, "FLOW_COMPLETED", "completed", beforeReplay);
    }

    [Fact]
    public async Task 重叠入口D_无票开启DE后只能继续E()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/d", selector: "DE", businessId: "biz");
        var state = AssertCommittedFirstStep(pipeline, start);
        Assert.Equal("DE", state.Variant);
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var wrongB = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        await AssertRejectedAsync(pipeline, wrongB, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 1);

        var restartD = await pipeline.SendAsync("/d", ticket: ticket, businessId: "biz");
        await AssertRejectedAsync(pipeline, restartD, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 1);

        var last = await pipeline.SendAsync("/e", ticket: ticket, businessId: "biz");
        AssertCompleted(pipeline, last, "DE", 2);
    }

    [Fact]
    public async Task 重叠入口D_持ADE第一步票推进到第二步后只能继续E()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ADE", businessId: "biz");
        var startState = AssertCommittedFirstStep(pipeline, start);
        Assert.Equal("ADE", startState.Variant);
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var stepD = await pipeline.SendAsync("/d", ticket: ticket, businessId: "biz");
        AssertAdvanced(pipeline, stepD, "ADE", 2);

        var wrongB = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        await AssertRejectedAsync(pipeline, wrongB, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 2);

        var last = await pipeline.SendAsync("/e", ticket: ticket, businessId: "biz");
        AssertCompleted(pipeline, last, "ADE", 3);
    }

    [Fact]
    public async Task 重叠入口D_DE与ADE两条组合互不干扰()
    {
        var pipeline = new Pipeline();
        var deStart = await pipeline.SendAsync("/d", selector: "DE", businessId: "biz");
        var deState = AssertCommittedFirstStep(pipeline, deStart);
        var deTicket = Assert.Single(deStart.Response.Headers["X-Flow-Ticket"]);
        Assert.Equal("DE", deState.Variant);

        var adeStart = await pipeline.SendAsync("/a", selector: "ADE", businessId: "biz");
        var adeState = AssertCommittedFirstStep(pipeline, adeStart, expectedForwardCount: 2);
        var adeTicket = Assert.Single(adeStart.Response.Headers["X-Flow-Ticket"]);
        Assert.Equal("ADE", adeState.Variant);
        Assert.NotEqual(deTicket, adeTicket);

        var adeStepD = await pipeline.SendAsync("/d", ticket: adeTicket, businessId: "biz");
        AssertAdvanced(pipeline, adeStepD, "ADE", 2);

        var deLast = await pipeline.SendAsync("/e", ticket: deTicket, businessId: "biz");
        AssertCompleted(pipeline, deLast, "DE", 2);

        var adeLast = await pipeline.SendAsync("/e", ticket: adeTicket, businessId: "biz");
        AssertCompleted(pipeline, adeLast, "ADE", 3);
    }

    [Theory]
    [InlineData("abe")]
    [InlineData(" abe ")]
    public async Task 持票推进携带一致前置参数_忽略大小写与空白放行(string selector)
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var advanced = await pipeline.SendAsync("/b", selector: selector, ticket: ticket, businessId: "biz");

        AssertAdvanced(pipeline, advanced, "ABE", 2);
    }

    [Theory]
    [InlineData("ADE")]
    [InlineData("unknown")]
    [InlineData("  ")]
    public async Task 持票推进携带冲突或非法前置参数_返回SelectorMismatch且不转发(string selector)
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync("/b", selector: selector, ticket: ticket, businessId: "biz");

        await AssertRejectedAsync(pipeline, rejected, "FLOW_SELECTOR_MISMATCH", "selector-mismatch", forwardedCount: 1);
    }

    [Fact]
    public async Task 持票推进同名前置参数多值_返回SelectorMismatch且不转发()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync(
            "/b",
            selector: new StringValues(["ABE", "ABE"]),
            ticket: ticket,
            businessId: "biz"
        );

        await AssertRejectedAsync(pipeline, rejected, "FLOW_SELECTOR_MISMATCH", "selector-mismatch", forwardedCount: 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("other")]
    public async Task 持票推进缺少或错误业务标识_返回BusinessMismatch且不转发(string? businessId)
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync("/b", ticket: ticket, businessId: businessId);

        await AssertRejectedAsync(pipeline, rejected, "FLOW_BUSINESS_MISMATCH", "business-mismatch", forwardedCount: 1);
    }

    [Fact]
    public async Task 持票跳步_返回StepNotAllowed且不转发()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync("/e", ticket: ticket, businessId: "biz");

        await AssertRejectedAsync(pipeline, rejected, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 1);
    }

    [Theory]
    [InlineData("/b")]
    [InlineData("/d")]
    public async Task 持票跨组合调用_返回StepNotAllowed且不转发(string path)
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ACE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync(path, ticket: ticket, businessId: "biz");

        await AssertRejectedAsync(pipeline, rejected, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 1);
    }

    [Theory]
    [InlineData("ABE", "/a")]
    [InlineData("DE", "/d")]
    public async Task 持未完成票据重调首步_返回StepNotAllowed且不转发(string variant, string path)
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync(path, selector: variant, businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var rejected = await pipeline.SendAsync(path, ticket: ticket, businessId: "biz");

        await AssertRejectedAsync(pipeline, rejected, "FLOW_STEP_NOT_ALLOWED", "step-not-allowed", forwardedCount: 1);
    }

    [Fact]
    public async Task 中间步下游非2xx_释放锁保留组合与顺位并可重试完成()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        pipeline.ResponseStatus = HttpStatusCode.BadGateway;
        var failed = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");

        Assert.Equal(StatusCodes.Status502BadGateway, failed.Response.StatusCode);
        Assert.False(failed.Response.Headers.ContainsKey("X-Flow-Ticket"));
        var afterFailure = Assert.IsType<InMemoryFlowStateStore.TicketState>(pipeline.Store.GetTicket(ticket));
        Assert.Equal("ABE", afterFailure.Variant);
        Assert.Equal(1, afterFailure.NextIndex);
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Active, afterFailure.Status);
        Assert.Empty(afterFailure.AttemptId);
        Assert.Equal(0, afterFailure.LockUntilUnixMs);

        pipeline.ResponseStatus = HttpStatusCode.OK;
        var retried = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        AssertAdvanced(pipeline, retried, "ABE", 2);

        var last = await pipeline.SendAsync("/e", ticket: ticket, businessId: "biz");
        AssertCompleted(pipeline, last, "ABE", 3);
    }

    [Fact]
    public async Task 同一步并发_仅一次放行其余返回InProgress()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var gate = new TaskCompletionSource();
        pipeline.ForwardGate = () => gate.Task;
        var first = pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        await pipeline.ForwardEntered.Task;

        var second = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        await AssertRejectedAsync(pipeline, second, "FLOW_IN_PROGRESS", "in-progress", forwardedCount: 2);

        gate.SetResult();
        var firstContext = await first;
        AssertAdvanced(pipeline, firstContext, "ABE", 2);
    }

    [Fact]
    public async Task 线性流程持票推进到完成_与参数化流程共存零回归()
    {
        var pipeline = new Pipeline();
        var start = await pipeline.SendAsync("/linear/start");
        var state = AssertCommittedFirstStep(pipeline, start);
        Assert.Equal("linear", state.FlowId);
        Assert.Empty(state.Variant);
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var end = await pipeline.SendAsync("/linear/end", ticket: ticket, businessId: "biz-linear");

        AssertCompleted(pipeline, end, string.Empty, 2);
    }

    [Fact]
    public async Task 已完成票据携带空白前置参数_优先返回SelectorMismatch且不转发()
    {
        var pipeline = new Pipeline();
        var ticket = await CompleteAbeFlowAsync(pipeline);

        var rejected = await pipeline.SendAsync("/e", selector: "  ", ticket: ticket, businessId: "biz");

        await AssertRejectedAsync(pipeline, rejected, "FLOW_SELECTOR_MISMATCH", "selector-mismatch", forwardedCount: 3);
    }

    [Fact]
    public async Task 已完成票据携带同名多值前置参数_优先返回SelectorMismatch且不转发()
    {
        var pipeline = new Pipeline();
        var ticket = await CompleteAbeFlowAsync(pipeline);

        var rejected = await pipeline.SendAsync(
            "/e",
            selector: new StringValues(["ABE", "ABE"]),
            ticket: ticket,
            businessId: "biz"
        );

        await AssertRejectedAsync(pipeline, rejected, "FLOW_SELECTOR_MISMATCH", "selector-mismatch", forwardedCount: 3);
    }

    [Fact]
    public async Task 已完成票据缺失业务标识_优先返回BusinessMismatch且不转发()
    {
        var pipeline = new Pipeline();
        var ticket = await CompleteAbeFlowAsync(pipeline);

        // 同时携带非法前置参数，验证业务标识校验先于前置参数校验。
        var rejected = await pipeline.SendAsync("/e", selector: "  ", ticket: ticket);

        await AssertRejectedAsync(pipeline, rejected, "FLOW_BUSINESS_MISMATCH", "business-mismatch", forwardedCount: 3);
    }

    [Fact]
    public async Task 同名多值业务标识_参数化与线性路径均取首个值()
    {
        var multiValueBusinessId = new StringValues(["biz", "other"]);

        var parameterized = new Pipeline();
        var startParam = await parameterized.SendAsync("/a", selector: "ABE", businessId: multiValueBusinessId);
        var paramState = AssertCommittedFirstStep(parameterized, startParam);
        Assert.Equal("biz", paramState.BusinessId);
        var paramTicket = Assert.Single(startParam.Response.Headers["X-Flow-Ticket"]);

        var advanced = await parameterized.SendAsync("/b", ticket: paramTicket, businessId: multiValueBusinessId);
        AssertAdvanced(parameterized, advanced, "ABE", 2);

        var linear = new Pipeline();
        var startLinear = await linear.SendAsync("/linear/start", businessId: multiValueBusinessId);
        var linearState = AssertCommittedFirstStep(linear, startLinear);
        Assert.Equal("biz", linearState.BusinessId);
        var linearTicket = Assert.Single(startLinear.Response.Headers["X-Flow-Ticket"]);

        var end = await linear.SendAsync("/linear/end", ticket: linearTicket, businessId: multiValueBusinessId);
        AssertCompleted(linear, end, string.Empty, 2);
    }

    private static async Task<string> CompleteAbeFlowAsync(Pipeline pipeline)
    {
        var start = await pipeline.SendAsync("/a", selector: "ABE", businessId: "biz");
        var ticket = Assert.Single(start.Response.Headers["X-Flow-Ticket"]);

        var middle = await pipeline.SendAsync("/b", ticket: ticket, businessId: "biz");
        AssertAdvanced(pipeline, middle, "ABE", 2);

        var last = await pipeline.SendAsync("/e", ticket: ticket, businessId: "biz");
        AssertCompleted(pipeline, last, "ABE", 3);

        return ticket;
    }

    [Fact]
    public async Task 与参数化流程共存时_线性首步无需前置参数并正常签发票据()
    {
        var pipeline = new Pipeline(HttpStatusCode.OK, "biz-linear");

        var context = await pipeline.SendAsync("/linear/start");

        var state = AssertCommittedFirstStep(pipeline, context);
        Assert.Equal("linear", state.FlowId);
        Assert.Empty(state.Variant);
        Assert.Equal("biz-linear", state.BusinessId);
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Business-Id"));
    }

    [Fact]
    public async Task 线性后续步骤无票_返回ProblemJson且不转发()
    {
        var pipeline = new Pipeline();

        var context = await pipeline.SendAsync("/linear/end");

        await AssertRejectedAsync(pipeline, context, "FLOW_NOT_FOUND", "not-found");
    }

    private static InMemoryFlowStateStore.TicketState AssertCommittedFirstStep(
        Pipeline pipeline,
        HttpContext context,
        HttpStatusCode status = HttpStatusCode.OK,
        int expectedForwardCount = 1
    )
    {
        Assert.Equal((int)status, context.Response.StatusCode);
        Assert.Equal(expectedForwardCount, pipeline.ForwardCount);
        var ticket = Assert.Single(context.Response.Headers["X-Flow-Ticket"]);
        Assert.False(string.IsNullOrWhiteSpace(ticket));
        Assert.Equal("1", Assert.Single(context.Response.Headers["X-Flow-Next-Index"]));
        Assert.Equal("no-store", Assert.Single(context.Response.Headers.CacheControl));
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Processing, pipeline.ForwardedTicket?.Status);
        Assert.Equal(0, pipeline.ForwardedTicket?.NextIndex);
        var state = Assert.IsType<InMemoryFlowStateStore.TicketState>(pipeline.Store.GetTicket(ticket));
        Assert.Equal(pipeline.ForwardedTicket?.Variant, state.Variant);
        Assert.Equal(1, state.NextIndex);
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Active, state.Status);
        Assert.Empty(state.AttemptId);
        Assert.Equal(0, state.LockUntilUnixMs);
        return state;
    }

    private static void AssertAdvanced(Pipeline pipeline, HttpContext context, string expectedVariant, int expectedNextIndex)
    {
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var ticket = Assert.Single(context.Response.Headers["X-Flow-Ticket"]);
        Assert.False(string.IsNullOrWhiteSpace(ticket));
        Assert.Equal(expectedNextIndex.ToString(), Assert.Single(context.Response.Headers["X-Flow-Next-Index"]));
        Assert.Equal("no-store", Assert.Single(context.Response.Headers.CacheControl));
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Completed"));
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Processing, pipeline.ForwardedTicket?.Status);
        Assert.Equal(expectedVariant, pipeline.ForwardedTicket?.Variant);
        Assert.Equal(expectedNextIndex - 1, pipeline.ForwardedTicket?.NextIndex);
        var state = Assert.IsType<InMemoryFlowStateStore.TicketState>(pipeline.Store.GetTicket(ticket));
        Assert.Equal(expectedVariant, state.Variant);
        Assert.Equal(expectedNextIndex, state.NextIndex);
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Active, state.Status);
        Assert.Empty(state.AttemptId);
        Assert.Equal(0, state.LockUntilUnixMs);
    }

    private static void AssertCompleted(Pipeline pipeline, HttpContext context, string expectedVariant, int expectedNextIndex)
    {
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("true", Assert.Single(context.Response.Headers["X-Flow-Completed"]));
        Assert.Equal("no-store", Assert.Single(context.Response.Headers.CacheControl));
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Ticket"));
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Next-Index"));
        var state = Assert.IsType<InMemoryFlowStateStore.TicketState>(
            pipeline.Store.GetTicket(pipeline.ForwardedAttempt.Ticket)
        );
        Assert.Equal(expectedVariant, state.Variant);
        Assert.Equal(expectedNextIndex, state.NextIndex);
        Assert.Equal(InMemoryFlowStateStore.TicketStatus.Completed, state.Status);
    }

    private static async Task AssertRejectedAsync(
        Pipeline pipeline,
        HttpContext context,
        string code,
        string slug,
        int forwardedCount = 0
    )
    {
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(forwardedCount, pipeline.ForwardCount);
        Assert.False(context.Response.Headers.ContainsKey("X-Flow-Ticket"));
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(403, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal($"https://errors.example.com/flow/{slug}", body.RootElement.GetProperty("type").GetString());
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
    }

    private sealed class Pipeline
    {
        private readonly FlowTicketMiddleware _middleware;

        public Pipeline(HttpStatusCode status = HttpStatusCode.OK, string responseBusinessId = null)
        {
            ResponseStatus = status;
            var options = FlowDefinitionRegistryVariantsTests.CreateOptions();
            var service = new FlowTicketService(Store, options.LockTtlSeconds);
            var transform = new FlowResponseTransform(options, service);
            _middleware = new FlowTicketMiddleware(
                async context =>
                {
                    ForwardCount++;
                    if (context.Items[FlowAttemptContext.ItemsKey] is FlowAttemptContext attempt)
                    {
                        ForwardedAttempt = attempt;
                        ForwardedTicket = Store.GetTicket(attempt.Ticket);
                    }

                    ForwardEntered.TrySetResult();
                    if (ForwardGate is not null)
                    {
                        await ForwardGate();
                    }

                    using var downstream = new HttpResponseMessage(ResponseStatus);
                    context.Response.StatusCode = (int)ResponseStatus;
                    if (responseBusinessId is not null)
                    {
                        downstream.Headers.Add("X-Flow-Business-Id", responseBusinessId);
                        context.Response.Headers["X-Flow-Business-Id"] = responseBusinessId;
                    }

                    await transform.ProcessAsync(new ResponseTransformContext
                    {
                        HttpContext = context,
                        ProxyResponse = downstream,
                    });
                },
                options,
                new FlowDefinitionRegistry(options),
                new DownstreamPathResolver(),
                service,
                NullLogger<FlowTicketMiddleware>.Instance
            );
        }

        public InMemoryFlowStateStore Store { get; } = new();

        public int ForwardCount { get; private set; }

        public HttpStatusCode ResponseStatus { get; set; }

        public Func<Task> ForwardGate { get; set; }

        public TaskCompletionSource ForwardEntered { get; } = new();

        public FlowAttemptContext ForwardedAttempt { get; private set; }

        public InMemoryFlowStateStore.TicketState ForwardedTicket { get; private set; }

        public async Task<HttpContext> SendAsync(
            string path,
            StringValues selector = default,
            string ticket = null,
            StringValues businessId = default
        )
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = $"/gateway{path}";
            context.Request.Headers["X-Mode"] = selector;
            context.Request.Headers["X-Flow-Ticket"] = ticket;
            context.Request.Headers["X-Business-Id"] = businessId;
            context.Response.Body = new MemoryStream();
            context.Features.Set<IReverseProxyFeature>(new ReverseProxyFeature
            {
                Route = new RouteModel(
                    new RouteConfig
                    {
                        RouteId = "gateway",
                        ClusterId = "downstream",
                        Match = new RouteMatch { Path = "/gateway/{**catch-all}" },
                        Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = "/gateway" }],
                    },
                    cluster: null,
                    transformer: HttpTransformer.Default
                ),
            });

            await _middleware.InvokeAsync(context);
            return context;
        }
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

internal sealed class InMemoryFlowStateStore(TimeProvider timeProvider = null) : IFlowStateStore
{
    private readonly Dictionary<string, TicketState> _tickets = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    public TicketState GetTicket(string ticket)
    {
        lock (_sync)
        {
            return FindTicket(ticket, _time.GetUtcNow().ToUnixTimeMilliseconds());
        }
    }

    public Task<FlowStoreResult> CreateAndReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var now = _time.GetUtcNow().ToUnixTimeMilliseconds();
            if (FindTicket(request.Ticket, now) is not null)
            {
                return Failed(FlowErrorCode.FlowInProgress);
            }

            _tickets.Add(request.Ticket, new TicketState(
                request.FlowId,
                request.BusinessId,
                request.ExpectedIndex,
                TicketStatus.Processing,
                request.AttemptId,
                now + request.LockTtlSeconds * 1000L,
                request.VariantKey,
                now + request.FlowTtlSeconds * 1000L
            ));
            return Success(request.ExpectedIndex);
        }
    }

    public Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var now = _time.GetUtcNow().ToUnixTimeMilliseconds();
            var state = FindTicket(request.Ticket, now);
            if (state is null)
            {
                return Failed(FlowErrorCode.FlowNotFound);
            }

            if (state.Status == TicketStatus.Completed)
            {
                return Failed(FlowErrorCode.FlowCompleted);
            }

            if (state.FlowId != request.FlowId)
            {
                return Failed(FlowErrorCode.FlowStepNotAllowed);
            }

            if (state.BusinessId.Length > 0 && state.BusinessId != request.BusinessId)
            {
                return Failed(FlowErrorCode.FlowBusinessMismatch);
            }

            var expectedIndex = request.ExpectedIndex;
            var matchedVariant = string.Empty;
            var candidates = request.CandidateVariantSteps ?? [];
            if (candidates.Count > 0)
            {
                if (state.Variant.Length == 0)
                {
                    return Failed(FlowErrorCode.FlowStepNotAllowed);
                }

                var selectorVariantKey = request.SelectorVariantKey ?? string.Empty;
                if (selectorVariantKey.Length > 0 && selectorVariantKey != state.Variant)
                {
                    return Failed(FlowErrorCode.FlowSelectorMismatch);
                }

                var candidate = candidates.FirstOrDefault(x => x.VariantKey == state.Variant);
                if (candidate is null)
                {
                    return Failed(FlowErrorCode.FlowStepNotAllowed);
                }

                expectedIndex = candidate.StepIndex;
                matchedVariant = candidate.VariantKey;
            }

            if (state.NextIndex != expectedIndex)
            {
                return Failed(FlowErrorCode.FlowStepNotAllowed);
            }

            if (state.Status == TicketStatus.Processing && state.LockUntilUnixMs > now)
            {
                return Failed(FlowErrorCode.FlowInProgress);
            }

            _tickets[request.Ticket] = state with
            {
                Status = TicketStatus.Processing,
                AttemptId = request.AttemptId,
                LockUntilUnixMs = now + request.LockTtlSeconds * 1000L,
            };
            return Success(state.NextIndex, variantKey: matchedVariant);
        }
    }

    public Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var now = _time.GetUtcNow().ToUnixTimeMilliseconds();
            var state = FindTicket(request.Ticket, now);
            if (state is null)
            {
                return Failed(FlowErrorCode.FlowNotFound);
            }

            if (state.Status == TicketStatus.Completed)
            {
                return Failed(FlowErrorCode.FlowCompleted);
            }

            if (state.Status != TicketStatus.Processing || state.AttemptId != request.AttemptId)
            {
                return Failed(FlowErrorCode.FlowInProgress);
            }

            if (state.NextIndex != request.ExpectedIndex)
            {
                return Failed(FlowErrorCode.FlowStepNotAllowed);
            }

            var businessId = state.BusinessId;
            if (!string.IsNullOrEmpty(request.BusinessIdFromResponse))
            {
                if (businessId.Length > 0 && businessId != request.BusinessIdFromResponse)
                {
                    return Failed(FlowErrorCode.FlowBusinessMismatch);
                }

                businessId = request.BusinessIdFromResponse;
            }

            var nextIndex = state.NextIndex + 1;
            _tickets[request.Ticket] = state with
            {
                BusinessId = businessId,
                NextIndex = nextIndex,
                Status = request.IsLastStep ? TicketStatus.Completed : TicketStatus.Active,
                AttemptId = string.Empty,
                LockUntilUnixMs = 0,
                ExpiresAtUnixMs = request.IsLastStep ? now + request.CompletedTtlSeconds * 1000L : state.ExpiresAtUnixMs,
            };
            return Success(nextIndex, request.IsLastStep);
        }
    }

    public Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var state = FindTicket(request.Ticket, _time.GetUtcNow().ToUnixTimeMilliseconds());
            if (state is null)
            {
                return Failed(FlowErrorCode.FlowNotFound);
            }

            if (state.Status == TicketStatus.Completed)
            {
                return Failed(FlowErrorCode.FlowCompleted);
            }

            if (state.AttemptId != request.AttemptId)
            {
                return Failed(FlowErrorCode.FlowInProgress);
            }

            _tickets[request.Ticket] = state with
            {
                Status = TicketStatus.Active,
                AttemptId = string.Empty,
                LockUntilUnixMs = 0,
            };
            return Success(state.NextIndex);
        }
    }

    public Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var state = FindTicket(request.Ticket, _time.GetUtcNow().ToUnixTimeMilliseconds());
            if (state is null)
            {
                return Failed(FlowErrorCode.FlowNotFound);
            }

            if (state.AttemptId != request.AttemptId)
            {
                return Failed(FlowErrorCode.FlowInProgress);
            }

            _tickets.Remove(request.Ticket);
            return Success(null);
        }
    }

    private TicketState FindTicket(string ticket, long now)
    {
        if (!_tickets.TryGetValue(ticket, out var state))
        {
            return null;
        }

        if (state.ExpiresAtUnixMs <= now)
        {
            _tickets.Remove(ticket);
            return null;
        }

        return state;
    }

    private static Task<FlowStoreResult> Failed(FlowErrorCode code) => Task.FromResult(FlowStoreResult.Failed(code));

    private static Task<FlowStoreResult> Success(int? nextIndex, bool completed = false, string? variantKey = null) =>
        Task.FromResult(FlowStoreResult.Success(nextIndex, completed, variantKey));

    internal sealed record TicketState(
        string FlowId,
        string BusinessId,
        int NextIndex,
        TicketStatus Status,
        string AttemptId,
        long LockUntilUnixMs,
        string Variant,
        long ExpiresAtUnixMs
    );

    internal enum TicketStatus
    {
        Active,
        Processing,
        Completed,
    }
}
