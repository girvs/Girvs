using System.Collections.Generic;

namespace Girvs.Infrastructure
{
    public class SingletonList<T> : Singleton<IList<T>>
    {
        static SingletonList()
        {
            Singleton<IList<T>>.Instance = new List<T>();
        }

        public static new IList<T> Instance => Singleton<IList<T>>.Instance;
    }
}