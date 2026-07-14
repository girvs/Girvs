using Girvs.Cache;
using Girvs.EventBus;

namespace Girvs.Aspire.Hosting.Tests;

public class DependsOnGraphTests
{
    [DependsOn(typeof(GirvsCacheModule), typeof(EventBusModule))]
    private class ServiceModule;

    [DependsOn(typeof(ServiceModule), typeof(GirvsCacheModule))]
    private class NestedModule;

    [DependsOn(typeof(CycleB))]
    private class CycleA;

    [DependsOn(typeof(CycleA))]
    private class CycleB;

    private class EmptyModule;

    [Fact]
    public void 收集直接声明的依赖模块()
    {
        var modules = DependsOnGraph.Collect(typeof(ServiceModule));

        Assert.Equal(2, modules.Count);
        Assert.Contains(typeof(GirvsCacheModule), modules);
        Assert.Contains(typeof(EventBusModule), modules);
        Assert.DoesNotContain(typeof(ServiceModule), modules);
    }

    [Fact]
    public void 递归收集嵌套依赖且去重()
    {
        var modules = DependsOnGraph.Collect(typeof(NestedModule));

        Assert.Contains(typeof(GirvsCacheModule), modules);
        Assert.Contains(typeof(EventBusModule), modules);
        Assert.Single(modules, type => type == typeof(GirvsCacheModule));
    }

    [Fact]
    public void 循环依赖不会导致死循环()
    {
        var modules = DependsOnGraph.Collect(typeof(CycleA));

        Assert.Contains(typeof(CycleB), modules);
    }

    [Fact]
    public void 无声明的模块返回空列表()
    {
        Assert.Empty(DependsOnGraph.Collect(typeof(EmptyModule)));
    }
}
