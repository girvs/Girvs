namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 静态实体管理者
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