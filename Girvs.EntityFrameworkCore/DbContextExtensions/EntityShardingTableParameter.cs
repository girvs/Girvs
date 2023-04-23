namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public class EntityShardingTableParameter
{
    public EntityShardingTableParameter(Type type)
    {
        EntityType = type;
    }

    /// <summary>
    /// 实体类型
    /// </summary>
    public Type EntityType { get; set; }

    /// <summary>
    /// 是否实现ITenantShardingTable接口
    /// </summary>
    public bool IsAssignableTenantShardingTable => EntityType.IsAssignableTo(typeof(ITenantShardingTable));

    /// <summary>
    /// 是否实现IYearShardingTable接口
    /// </summary>
    public bool IsAssignableYearShardingTable => EntityType.IsAssignableTo(typeof(IYearShardingTable));

    /// <summary>
    /// 是否需要进行分表
    /// </summary>
    public bool IsNeedShardingTable => IsAssignableTenantShardingTable || IsAssignableYearShardingTable;

    /// <summary>
    /// 获取当前实体的分表名称
    /// </summary>
    /// <returns></returns>
    public string GetCurrentShardingTableName()
    {
        return $"{EntityType.Name}{GetCurrentShrdingTableSuffix()}";
    }

    /// <summary>
    /// 获取当前分表的后缀名称
    /// </summary>
    /// <returns></returns>
    public string GetCurrentShrdingTableSuffix()
    {
        return EngineContext.Current.GetTenantShardingTableSuffix() +
               EngineContext.Current.GetYearShardingTableSuffix();
    }
}