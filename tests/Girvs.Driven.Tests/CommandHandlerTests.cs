namespace Girvs.Driven.Tests;

public class CommandHandlerTests
{
    private sealed class NoUowCommandHandler(IMediatorHandler bus) : CommandHandler(bus)
    {
        public void ExposeNotifyValidationErrors(Command message)
        {
            NotifyValidationErrors(message);
        }
    }

    [Fact]
    public void 单参构造_可创建处理器()
    {
        var handler = new NoUowCommandHandler(new FakeMediatorHandler());
        Assert.NotNull(handler);
    }

    [Fact]
    public void 通知校验错误_通过总线发布领域通知()
    {
        var bus = new FakeMediatorHandler();
        var handler = new NoUowCommandHandler(bus);

        var validationResult = new ValidationResult(
            [new ValidationFailure("Desc", "Desc 不能为空")]
        );
        var command = new FakePlainCommand("desc") with { ValidationResult = validationResult };

        handler.ExposeNotifyValidationErrors(command);

        var notification = Assert.IsType<DomainNotification>(Assert.Single(bus.RaisedEvents));
        Assert.Equal("Desc 不能为空", notification.Value);
    }

    [Fact]
    public void 缓存命令处理器_使用单参构造()
    {
        // RemoveByKeyCommandHandler / RemoveByPrefixCommandHandler 改用 CommandHandler(bus) 构造
        var handler = new RemoveByKeyCommandHandler(null, new FakeMediatorHandler());
        Assert.NotNull(handler);
    }
}
