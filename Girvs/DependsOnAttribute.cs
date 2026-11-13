namespace Girvs;

/// <summary>
/// 声明模块间的依赖关系。业务服务定义根模块类并声明其使用的 Girvs 组件模块，
/// 由 Girvs.Aspire.Hosting 的 AddGirvsProject 读取以完成 AppHost 资源自动编排。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DependsOnAttribute : Attribute
{
    public Type[] DependedModuleTypes { get; }

    public DependsOnAttribute(params Type[] dependedModuleTypes)
    {
        DependedModuleTypes = dependedModuleTypes ?? Type.EmptyTypes;
    }
}
