namespace Girvs.TypeFinder;

/// <summary>
/// 实现此接口的类提供有关类型的信息到系统引擎中的各种服务。
/// </summary>
public interface ITypeFinder
{
    IEnumerable<Type> FindOfType<T>(FindType findType = FindType.ConcreteClasses);

    IEnumerable<Type> FindOfType(Type assignTypeFrom, FindType findType = FindType.ConcreteClasses);

    IList<Assembly> GetAssemblies();
}


public enum FindType
{
    /// <summary>
    /// 实现的类
    /// </summary>
    ConcreteClasses,
        
    /// <summary>
    /// 接口
    /// </summary>
    Interface,
        
    /// <summary>
    /// 抽象类
    /// </summary>
    AbstractClasses
}