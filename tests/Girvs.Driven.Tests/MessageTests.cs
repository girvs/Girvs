namespace Girvs.Driven.Tests;

public class MessageTests
{
    [Fact]
    public void 构造Message_引擎未配置服务_不得抛出异常()
    {
        // 测试环境未配置 EngineContext 的服务容器：PrincipalAccessor 解析为 null，
        // 构造内的异常必须被安全吞掉（含记日志失败的情形），确保热路径不抛出
        var message = new Message<bool>();

        Assert.Equal(typeof(Message<bool>).Name, message.MessageType);
        Assert.Equal(Guid.Empty, message.AggregateId);
    }

    [Fact]
    public void 构造Message_消息类型为类型名()
    {
        var message = new FakeStringCommand("desc");

        Assert.Equal(nameof(FakeStringCommand), message.MessageType);
    }
}
