namespace Girvs.Infrastructure;

/// <summary>
/// 静态编译的“单例”，用于在应用程序域的整个生命周期中存储对象。模式的单词意义上没有太多的单例作为存储单个实例的标准化方法。
/// </summary>
public class Singleton<T> : BaseSingleton
{
    private static readonly object SyncRoot = new();

    // 以 object 承载实例：既保留无约束的泛型 API，又能借助 Volatile 保证引用发布的可见性，
    // 读取快路径无需进入 monitor。
    private static object _instance;

    public static T Instance
    {
        get
        {
            var instance = Volatile.Read(ref _instance);
            if (instance is not null)
                return (T)instance;

            lock (SyncRoot)
            {
                // 值类型未设置时返回 default(T)，与原实现保持一致。
                return _instance is null ? default : (T)_instance;
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _instance = value;
                if (value is null)
                    AllSingletons.Remove(typeof(T));
                else
                    AllSingletons[typeof(T)] = value;
            }
        }
    }
}
