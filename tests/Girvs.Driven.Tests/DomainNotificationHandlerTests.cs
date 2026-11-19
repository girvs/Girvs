namespace Girvs.Driven.Tests;

public class DomainNotificationHandlerTests
{
    [Fact]
    public void 实现IDisposable接口()
    {
        Assert.True(new DomainNotificationHandler() is IDisposable);
    }

    [Fact]
    public void Handle_添加通知_HasNotifications为真()
    {
        var handler = new DomainNotificationHandler();
        Assert.False(handler.HasNotifications());

        handler.Handle(new DomainNotification("key", "value"), CancellationToken.None);

        Assert.True(handler.HasNotifications());
        var notification = Assert.Single(handler.GetNotifications());
        Assert.Equal("key", notification.Key);
        Assert.Equal("value", notification.Value);
    }

    [Fact]
    public void GetNotificationMessage_按Key分组聚合()
    {
        var handler = new DomainNotificationHandler();
        handler.Handle(new DomainNotification("a", "1"), CancellationToken.None);
        handler.Handle(new DomainNotification("a", "2"), CancellationToken.None);
        handler.Handle(new DomainNotification("b", "3"), CancellationToken.None);

        var messages = handler.GetNotificationMessage();

        Assert.Equal(2, messages.Count);
        Assert.Equal(new[] { "1", "2" }, messages["a"]);
        Assert.Equal(new[] { "3" }, messages["b"]);
    }

    [Fact]
    public void Handle_并发添加通知_全部计入且不抛异常()
    {
        var handler = new DomainNotificationHandler();

        Parallel.For(
            0,
            200,
            i =>
                handler
                    .Handle(new DomainNotification("key", i.ToString()), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult()
        );

        Assert.Equal(200, handler.GetNotifications().Count());
    }

    [Fact]
    public void Dispose_清空全部通知()
    {
        var handler = new DomainNotificationHandler();
        handler.Handle(new DomainNotification("key", "value"), CancellationToken.None);
        Assert.True(handler.HasNotifications());

        handler.Dispose();

        Assert.False(handler.HasNotifications());
        Assert.Empty(handler.GetNotifications());
    }
}
