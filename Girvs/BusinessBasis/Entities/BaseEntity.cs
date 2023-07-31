namespace Girvs.BusinessBasis.Entities;

public abstract class BaseEntity : BaseEntity<Guid>
{
}

/// <summary>
/// 所有实体基类
/// </summary>
public abstract class BaseEntity<TPrimaryKey> : Entity<TPrimaryKey>
{
    protected BaseEntity()
    {
        FormatPrpertyValue();
    }

    /// <summary>
    /// 初始化系统字段值
    /// </summary>
    private void FormatPrpertyValue()
    {
        if (this is IIncludeCreateTime createTimeObj)
        {
            createTimeObj.CreateTime = DateTime.Now;
        }

        if (this is IIncludeUpdateTime updateTimeObj)
        {
            updateTimeObj.UpdateTime = DateTime.Now;
        }

        if (this is IIncludeDeleteField deleteFieldObj)
        {
            deleteFieldObj.IsDelete = false;
        }

        if (this is IIncludeInitField initFieldObj)
        {
            initFieldObj.IsInitData = false;
        }

        var identityClaim = EngineContext.Current.ClaimManager?.IdentityClaim;

        if (identityClaim != null)
        {
            if (this is IIncludeCreatorName creatorNameObj)
            {
                var creatorName = EngineContext.Current.ClaimManager.IdentityClaim.UserName;
                if (!string.IsNullOrEmpty(creatorName))
                {
                    creatorNameObj.CreatorName = creatorName;
                }
            }

            if (this is IIncludeMultiTenantName multiTenantNameObj)
            {
                var tenantName = EngineContext.Current.ClaimManager.IdentityClaim.TenantName;
                if (!string.IsNullOrEmpty(tenantName))
                {
                    multiTenantNameObj.TenantName = tenantName;
                }
            }

            var multiTenantPrperty = GetType().GetProperty(nameof(IIncludeMultiTenant<object>.TenantId));
            if (multiTenantPrperty != null)
            {
                var tenantIdStr = EngineContext.Current.ClaimManager.IdentityClaim.TenantId;
                if (!string.IsNullOrEmpty(tenantIdStr))
                {
                    var value = GirvsConvert.ToSpecifiedType(multiTenantPrperty.PropertyType.FullName,
                        tenantIdStr);
                    multiTenantPrperty.SetValue(this, value);
                }
            }

            var creatorPrperty = GetType().GetProperty(nameof(IIncludeCreatorId<object>.CreatorId));
            if (creatorPrperty != null)
            {
                var currentUserIdStr = EngineContext.Current.ClaimManager.IdentityClaim.UserId;
                if (!string.IsNullOrEmpty(currentUserIdStr))
                {
                    var value = GirvsConvert.ToSpecifiedType(creatorPrperty.PropertyType.FullName,
                        currentUserIdStr);
                    creatorPrperty.SetValue(this, value);
                }
            }
        }
    }

    /// <summary>
    /// 主键值
    /// </summary>
    public TPrimaryKey Id { get; set; }

    /// <summary>
    /// 重写方法 相等运算
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        var compareTo = obj as BaseEntity<TPrimaryKey>;

        if (ReferenceEquals(this, compareTo)) return true;
        if (ReferenceEquals(null, compareTo)) return false;

        return Id.Equals(compareTo.Id);
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
        return (GetType().GetHashCode() * 907) + Id.GetHashCode();
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