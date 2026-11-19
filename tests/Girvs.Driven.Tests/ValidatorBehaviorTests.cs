namespace Girvs.Driven.Tests;

public class ValidatorBehaviorTests
{
    private static ValidatorBehavior<FakeCommand, bool> CreateBehavior(
        FakeMediatorHandler bus,
        params IValidator<FakeCommand>[] validators
    )
    {
        return new ValidatorBehavior<FakeCommand, bool>(
            bus,
            validators,
            NullLogger<ValidatorBehavior<FakeCommand, bool>>.Instance
        );
    }

    [Fact]
    public async Task 真实DI容器_开放泛型注册_可解析ValidatorBehavior()
    {
        // 验证改为注入 IEnumerable<IValidator<TRequest>> 后，开放泛型管道注册仍可由 DI 容器正常构造
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMediatorHandler>(new FakeMediatorHandler());
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

        using var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<FakeCommand, bool>>();

        Assert.NotNull(behavior);
        var result = await behavior.Handle(
            new FakeCommand("desc"),
            _ => Task.FromResult(true),
            CancellationToken.None
        );
        Assert.True(result);
    }

    [Fact]
    public async Task 已注册GirvsCommandValidator_应用其规则并抛出GirvsException()
    {
        var bus = new FakeMediatorHandler();
        var behavior = CreateBehavior(bus, new FakeGirvsCommandValidator());

        var exception = await Assert.ThrowsAsync<GirvsException>(
            () =>
                behavior.Handle(
                    new FakeCommand(""),
                    _ => Task.FromResult(true),
                    CancellationToken.None
                )
        );

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task 校验失败且延迟模式_发布领域通知并返回默认值()
    {
        var bus = new FakeMediatorHandler();
        var validator = new FakeGirvsCommandValidator { IsErrorMessageDelay = true };
        var behavior = CreateBehavior(bus, validator);
        var nextCalled = false;

        var result = await behavior.Handle(
            new FakeCommand(""),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(true);
            },
            CancellationToken.None
        );

        Assert.False(result);
        Assert.False(nextCalled);
        var notification = Assert.IsType<DomainNotification>(Assert.Single(bus.RaisedEvents));
        Assert.Equal("Desc", notification.Key);
    }

    [Fact]
    public async Task 仅注册非Girvs校验器_回退默认校验器_请求直接通过()
    {
        // PlainFailingValidator 的规则恒不通过；若被应用则必然失败，
        // 请求成功返回说明已回退到无规则的默认校验器
        var bus = new FakeMediatorHandler();
        var behavior = CreateBehavior(bus, new PlainFailingValidator());

        var result = await behavior.Handle(
            new FakeCommand("desc"),
            _ => Task.FromResult(true),
            CancellationToken.None
        );

        Assert.True(result);
    }

    [Fact]
    public async Task 未注册任何校验器_回退默认校验器_请求直接通过()
    {
        var behavior = CreateBehavior(new FakeMediatorHandler());

        var result = await behavior.Handle(
            new FakeCommand("desc"),
            _ => Task.FromResult(true),
            CancellationToken.None
        );

        Assert.True(result);
    }
}
