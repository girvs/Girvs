namespace Girvs.ServiceGovernance.Tests;

public class DependsOnAttributeTests
{
    [DependsOn(typeof(string), typeof(int))]
    private class FakeModule;

    private class NoDependsModule;

    [Fact]
    public void 属性保存声明的依赖模块类型()
    {
        var attribute = typeof(FakeModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), false)
            .Cast<DependsOnAttribute>()
            .Single();

        Assert.Equal(new[] { typeof(string), typeof(int) }, attribute.DependedModuleTypes);
    }

    [Fact]
    public void 传入null时依赖列表为空数组()
    {
        var attribute = new DependsOnAttribute(null);

        Assert.NotNull(attribute.DependedModuleTypes);
        Assert.Empty(attribute.DependedModuleTypes);
    }

    [Fact]
    public void 未声明属性的类读取结果为空()
    {
        Assert.Empty(
            typeof(NoDependsModule).GetCustomAttributes(typeof(DependsOnAttribute), false)
        );
    }
}
