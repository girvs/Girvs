namespace Girvs.Driven.Tests;

public class CommandOperateBehaviorTests
{
    [Fact]
    public async Task Handle_响应为引用类型null_不应抛空引用异常()
    {
        // 旧行为：result 为 null 时 result.Equals(default) 直接 NRE，本用例锁定修复后的行为
        var behavior = new CommandOperateBehavior<FakeStringCommand, string>(
            NullLogger<CommandOperateBehavior<FakeStringCommand, string>>.Instance
        );

        var result = await behavior.Handle(
            new FakeStringCommand("desc"),
            _ => Task.FromResult<string>(null),
            CancellationToken.None
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_响应为默认值_不触发命令操作()
    {
        var behavior = new CommandOperateBehavior<FakeCommand, bool>(
            NullLogger<CommandOperateBehavior<FakeCommand, bool>>.Instance
        );

        // bool 默认值为 false，应跳过操作处理并原样返回
        var result = await behavior.Handle(
            new FakeCommand("desc"),
            _ => Task.FromResult(false),
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_响应非默认值且无操作处理器_正常返回()
    {
        // 测试环境未注册 ICommandOperateHandler，Resolve 返回 null，
        // await null?.Handle 应保持不抛并返回结果；
        // 使用非泛型 Command 子类以进入"request is Command"分支
        var behavior = new CommandOperateBehavior<FakePlainCommand, bool>(
            NullLogger<CommandOperateBehavior<FakePlainCommand, bool>>.Instance
        );

        var result = await behavior.Handle(
            new FakePlainCommand("desc"),
            _ => Task.FromResult(true),
            CancellationToken.None
        );

        Assert.True(result);
    }
}
