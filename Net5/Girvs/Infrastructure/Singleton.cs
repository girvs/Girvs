namespace Girvs.Infrastructure
{
    /// <summary>
    /// 静态编译的“单例”，用于在应用程序域的整个生命周期中存储对象。模式的单词意义上没有太多的单例作为存储单个实例的标准化方法。
    /// </summary>
    public class Singleton<T> : BaseSingleton
    {
        private static T instance;

        public static T Instance
        {
            get => instance;
            set
            {
                instance = value;
                AllSingletons[typeof(T)] = value;
            }
        }
    }
}
