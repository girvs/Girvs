namespace Girvs.Infrastructure;

/// <summary>
/// 提供对<see cref =“ Singleton {T}” >>存储的所有“单例”的访问。
/// </summary>
public class BaseSingleton
{
    static BaseSingleton()
    {
        AllSingletons = new Dictionary<Type, object>();
    }

    /// <summary>
    /// 类型字典到单例实例
    /// </summary>
    public static IDictionary<Type, object> AllSingletons { get; }
}