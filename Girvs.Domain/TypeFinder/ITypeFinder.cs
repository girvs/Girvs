using System;
using System.Collections.Generic;
using System.Reflection;

namespace Girvs.Domain.TypeFinder
{
    /// <summary>
    /// 实现此接口的类提供有关类型的信息到系统引擎中的各种服务。
    /// </summary>
    public interface ITypeFinder
    {
        /// <summary>
        /// 查找类型的类别
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="onlyConcreteClasses">指示是否仅查找具体类的值</param>
        /// <returns>Result</returns>
        IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true, bool includeInterFace = false);

        /// <summary>
        /// 查找类型的类别
        /// </summary>
        /// <param name="assignTypeFrom">从分配类型</param>
        /// <param name="onlyConcreteClasses">指示是否仅查找具体类的值</param>
        /// <returns>Result</returns>
        /// <returns></returns>
        IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true, bool includeInterFace = false);

        /// <summary>
        /// 查找类型的类别
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="assemblies">Assemblies</param>
        /// <param name="onlyConcreteClasses">指示是否仅查找具体类的值</param>
        /// <returns>Result</returns>
        IEnumerable<Type> FindClassesOfType<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true, bool includeInterFace = false);

        /// <summary>
        /// 查找类型的类别
        /// </summary>
        /// <param name="assignTypeFrom">Assign type from</param>
        /// <param name="assemblies">Assemblies</param>
        /// <param name="onlyConcreteClasses">指示是否仅查找具体类的值</param>
        /// <returns>Result</returns>
        IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true, bool includeInterFace = false);

        /// <summary>
        /// 获取与当前实现相关的程序集.
        /// </summary>
        /// <returns>程序集列表</returns>
        IList<Assembly> GetAssemblies();
    }
}