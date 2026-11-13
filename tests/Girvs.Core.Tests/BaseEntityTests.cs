using System;
using Girvs.BusinessBasis.Entities;
using Xunit;

namespace Girvs.Core.Tests;

[Collection(EngineContextCollection.Name)]
public class BaseEntityTests
{
    [Fact]
    public void 构造实体_填充系统时间字段()
    {
        var before = DateTime.Now;

        var entity = new TimestampEntity();

        Assert.True(entity.CreateTime >= before);
        Assert.True(entity.UpdateTime >= before);
    }

    [Fact]
    public void 构造实体_无身份上下文_租户与创建人保持默认()
    {
        var entity = new OwnerEntity();

        Assert.Equal(Guid.Empty, entity.TenantId);
        Assert.Equal(Guid.Empty, entity.CreatorId);
    }

    [Fact]
    public void 引用类型主键为空_相等比较不抛异常()
    {
        var left = new ReferenceKeyEntity();
        var right = new ReferenceKeyEntity();

        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void 主键相同_实体相等()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TimestampEntity {Id = id}, new TimestampEntity {Id = id});
    }

    [Fact]
    public void 主键不同_实体不相等()
    {
        Assert.NotEqual(
            new TimestampEntity {Id = Guid.NewGuid()},
            new TimestampEntity {Id = Guid.NewGuid()}
        );
    }

    private sealed class TimestampEntity
        : BaseEntity<Guid>,
            IIncludeCreateTime,
            IIncludeUpdateTime,
            IIncludeDeleteField,
            IIncludeInitField
    {
        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public bool IsDelete { get; set; }

        public bool IsInitData { get; set; }
    }

    private sealed class OwnerEntity : BaseEntity<Guid>, IIncludeCreatorId<Guid>, IIncludeMultiTenant<Guid>
    {
        public Guid CreatorId { get; set; }

        public Guid TenantId { get; set; }
    }

    private sealed class ReferenceKeyEntity : BaseEntity<string> { }
}
