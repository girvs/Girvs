namespace Girvs.EntityFrameworkCore.EntityConfigurations;

public abstract class GirvsAbstractEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, Entity
{
    // 方法上的类型参数与类级 TEntity 语义不同（需约束为 BaseEntity<TKey>），
    // 改名以避免 CS0693 屏蔽警告；泛型参数名不影响方法签名与调用方。
    protected virtual void OnModelCreatingTable<TEntityModel, TKey>(EntityTypeBuilder<TEntityModel> builder)
        where TEntityModel : BaseEntity<TKey>, new()
    {
        var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter<TEntityModel>();
        builder.ToTable(entityShardingSet.GetCurrentShardingTableName());
    }

    protected virtual void OnModelCreatingKey<TEntityModel, TKey>(EntityTypeBuilder<TEntityModel> builder)
        where TEntityModel : BaseEntity<TKey>, new()
    {
        builder.HasKey(x => x.Id);
    }

    protected virtual void OnModelCreatingTableKey<TEntityModel, TKey>(EntityTypeBuilder<TEntityModel> builder)
        where TEntityModel : BaseEntity<TKey>, new()
    {
        OnModelCreatingTable<TEntityModel, TKey>(builder);
        OnModelCreatingKey<TEntityModel, TKey>(builder);
    }

    protected virtual void OnModelCreatingBaseEntity<TEntityModel, TKey>(EntityTypeBuilder<TEntityModel> builder)
        where TEntityModel : BaseEntity<TKey>, new()
    {
        foreach (var propertyInfo in typeof(TEntityModel).GetProperties())
        {
            switch (propertyInfo.Name)
            {
                case nameof(IIncludeCreateTime.CreateTime):
                    builder.Property(nameof(IIncludeCreateTime.CreateTime)).HasColumnType("datetime");
                    break;

                case nameof(IIncludeTamperProof.DataCheckCode):
                    builder.Property(nameof(IIncludeTamperProof.DataCheckCode)).HasColumnType("varchar(36)");
                    break;

                case nameof(IIncludeUpdateTime.UpdateTime):
                    builder.Property(nameof(IIncludeUpdateTime.UpdateTime)).HasColumnType("datetime");
                    break;

                case nameof(IIncludeInitField.IsInitData):
                    builder.Property(nameof(IIncludeInitField.IsInitData)).HasColumnType("bit");
                    break;

                case nameof(IIncludeMultiTenant<object>.TenantId):
                    ConfigureIdProperty(
                        builder,
                        nameof(IIncludeMultiTenant<object>.TenantId),
                        propertyInfo.PropertyType
                    );
                    break;

                case nameof(IIncludeDeleteField.IsDelete):
                    builder.Property(nameof(IIncludeDeleteField.IsDelete)).HasColumnType("bit");
                    break;

                case nameof(IIncludeCreatorId<object>.CreatorId):
                    ConfigureIdProperty(
                        builder,
                        nameof(IIncludeCreatorId<object>.CreatorId),
                        propertyInfo.PropertyType
                    );
                    break;

                case nameof(IIncludeCreatorName.CreatorName):
                    builder.Property(nameof(IIncludeCreatorName.CreatorName)).HasColumnType("nvarchar(40)");
                    break;
            }
        }
    }

    private static void ConfigureIdProperty<TEntityModel>(
        EntityTypeBuilder<TEntityModel> builder,
        string propertyName,
        Type propertyType
    )
        where TEntityModel : class
    {
        if (propertyType == typeof(Guid) || propertyType == typeof(string))
        {
            builder.Property(propertyName).HasColumnType("varchar(36)");
        }
        else if (propertyType == typeof(int))
        {
            builder.Property(propertyName).HasColumnType("number");
        }
    }

    public virtual void OnModelCreatingBaseEntityAndTableKey<TEntityModel, TKey>(
        EntityTypeBuilder<TEntityModel> builder
    )
        where TEntityModel : BaseEntity<TKey>, new()
    {
        OnModelCreatingTableKey<TEntityModel, TKey>(builder);
        OnModelCreatingBaseEntity<TEntityModel, TKey>(builder);
    }

    public abstract void Configure(EntityTypeBuilder<TEntity> builder);
}
