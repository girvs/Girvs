namespace Girvs.Driven.Tests;

public class CommandTests
{
    [Fact]
    public async Task 构造命令_无HTTP上下文_不抛异常且返回结果()
    {
        // 测试环境无 HTTP 上下文，命令构造走本地 DNS 解析分支；
        // DNS 失败时也应降级为 null 而不是抛出异常
        var behavior = new CommandOperateBehavior<FakeCommand, bool>(
            NullLogger<CommandOperateBehavior<FakeCommand, bool>>.Instance
        );

        var result = await behavior.Handle(
            new FakeCommand("desc"),
            _ => Task.FromResult(true),
            CancellationToken.None
        );

        Assert.True(result);
    }

    [Fact]
    public void 构造命令_本地IP地址解析结果被缓存()
    {
        // 同进程内多次构造命令，OperateIpAddress 应来自同一份缓存（值一致），
        // 且无论 DNS 成功与否，构造都不得抛出异常
        var first = new FakeCommand("first");
        var second = new FakeCommand("second");

        Assert.Equal(first.OperateIpAddress, second.OperateIpAddress);
    }

    [Fact]
    public void 构造非泛型Command_默认属性值正确()
    {
        var command = new FakePlainCommand("desc");

        Assert.Equal("desc", command.CommandDesc);
    }
}
