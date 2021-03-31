using System;
using System.Collections.Generic;
using System.Reflection;

namespace Girvs.TypeFinder
{
    /// <summary>
    /// 实现此接口的类提供有关类型的信息到系统引擎中的各种服务。
    /// </summary>
    public interface ITypeFinder
    {
        IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true);

        IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true);

        IList<Assembly> GetAssemblies();
    }
}