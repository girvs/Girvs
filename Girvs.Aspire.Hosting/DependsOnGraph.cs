namespace Girvs.Aspire.Hosting;

public static class DependsOnGraph
{
    /// <summary>
    /// 深度优先收集根模块声明的全部依赖模块类型（含递归依赖），去重且防循环。
    /// 返回结果不含根模块本身。
    /// </summary>
    public static IReadOnlyList<Type> Collect(Type rootModuleType)
    {
        ArgumentNullException.ThrowIfNull(rootModuleType);

        var visited = new HashSet<Type>();
        var result = new List<Type>();
        Visit(rootModuleType, visited, result);
        result.Remove(rootModuleType);
        return result;
    }

    private static void Visit(Type moduleType, HashSet<Type> visited, List<Type> result)
    {
        if (!visited.Add(moduleType))
            return;

        result.Add(moduleType);

        var dependedTypes = moduleType
            .GetCustomAttributes<DependsOnAttribute>(false)
            .SelectMany(attribute => attribute.DependedModuleTypes);

        foreach (var dependedType in dependedTypes)
            Visit(dependedType, visited, result);
    }
}
