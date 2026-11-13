using System.Collections.Concurrent;

namespace Girvs.BusinessBasis.Entities;

public abstract class BaseEntity : BaseEntity<Guid> { }

/// <summary>
/// 所有实体基类
/// </summary>
public abstract class BaseEntity<TPrimaryKey> : Entity<TPrimaryKey>
{
    /// <summary>
    /// 按实体类型缓存反射结果，避免每次实例化都重复查找属性与判断接口。
    /// </summary>
    private static readonly ConcurrentDictionary<Type, EntityFormatMetadata> MetadataCache = new();

    protected BaseEntity()
    {
        FormatPrpertyValue();
    }

    /// <summary>
    /// 初始化系统字段值
    /// </summary>
    private void FormatPrpertyValue()
    {
        var metadata = MetadataCache.GetOrAdd(GetType(), static type => CreateMetadata(type));

        // 只取一次当前时间，避免 createTime/updateTime 出现微秒级差异，也少一次系统调用。
        var now = DateTime.Now;

        if (this is IIncludeCreateTime createTimeObj)
        {
            createTimeObj.CreateTime = now;
        }

        if (this is IIncludeUpdateTime updateTimeObj)
        {
            updateTimeObj.UpdateTime = now;
        }

        if (this is IIncludeDeleteField deleteFieldObj)
        {
            deleteFieldObj.IsDelete = false;
        }

        if (this is IIncludeInitField initFieldObj)
        {
            initFieldObj.IsInitData = false;
        }

        if (!metadata.NeedsPrincipal)
            return;

        var principal = EngineContext.Current.PrincipalAccessor?.Principal;

        if (principal == null)
            return;

        if (this is IIncludeCreatorName creatorNameObj)
        {
            var creatorName = principal.GetUserName();
            if (!string.IsNullOrEmpty(creatorName))
            {
                creatorNameObj.CreatorName = creatorName;
            }
        }

        if (this is IIncludeMultiTenantName multiTenantNameObj)
        {
            var tenantName = principal.GetTenantName();
            if (!string.IsNullOrEmpty(tenantName))
            {
                multiTenantNameObj.TenantName = tenantName;
            }
        }

        TrySetConvertedValue(this, metadata.TenantId, principal.GetTenantId());
        TrySetConvertedValue(this, metadata.CreatorId, principal.GetUserId());
    }

    private static EntityFormatMetadata CreateMetadata(Type type)
    {
        var tenantIdProperty = type.GetProperty(nameof(IIncludeMultiTenant<object>.TenantId));
        var creatorIdProperty = type.GetProperty(nameof(IIncludeCreatorId<object>.CreatorId));

        return new EntityFormatMetadata
        {
            TenantId = tenantIdProperty,
            CreatorId = creatorIdProperty,
            NeedsPrincipal =
                tenantIdProperty != null
                || creatorIdProperty != null
                || typeof(IIncludeCreatorName).IsAssignableFrom(type)
                || typeof(IIncludeMultiTenantName).IsAssignableFrom(type)
        };
    }

    private static void TrySetConvertedValue(object target, PropertyInfo property, string rawValue)
    {
        if (property == null || !property.CanWrite || string.IsNullOrEmpty(rawValue))
            return;

        try
        {
            var value = GirvsConvert.ToSpecifiedType(property.PropertyType, rawValue);
            property.SetValue(target, value);
        }
        catch (Exception exception)
            when (exception is GirvsException
                      or FormatException
                      or InvalidCastException
                      or OverflowException
                      or ArgumentException
            )
        {
            // 系统字段填充失败时保留默认值，避免实体构造因身份信息格式异常而中断。
        }
    }

    private sealed class EntityFormatMetadata
    {
        public PropertyInfo TenantId { get; init; }

        public PropertyInfo CreatorId { get; init; }

        public bool NeedsPrincipal { get; init; }
    }

    /// <summary>
    /// 主键值
    /// </summary>
    public virtual TPrimaryKey Id { get; set; }

    /// <summary>
    /// 重写方法 相等运算
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        var compareTo = obj as BaseEntity<TPrimaryKey>;

        if (ReferenceEquals(this, compareTo))
            return true;
        if (ReferenceEquals(null, compareTo))
            return false;

        return EqualityComparer<TPrimaryKey>.Default.Equals(Id, compareTo.Id);
    }

    /// <summary>
    /// 重写方法 实体比较 ==
    /// </summary>
    /// <param name="a">领域实体a</param>
    /// <param name="b">领域实体b</param>
    /// <returns></returns>
    public static bool operator ==(BaseEntity<TPrimaryKey> a, BaseEntity<TPrimaryKey> b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            return true;

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// 重写方法 实体比较 !=
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator !=(BaseEntity<TPrimaryKey> a, BaseEntity<TPrimaryKey> b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 获取哈希
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        var idHash = EqualityComparer<TPrimaryKey>.Default.GetHashCode(Id);
        return (GetType().GetHashCode() * 907) + idHash;
    }

    /// <summary>
    /// 输出领域对象的状态
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return GetType().Name + " [Id=" + Id + "]";
    }
}
