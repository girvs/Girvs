using System.ComponentModel.DataAnnotations.Schema;
using Girvs.BusinessBasis.Entities;

namespace Sample.ServiceA.Entities;

/// <summary>
/// 最小实体：仅继承 AggregateRoot&lt;Guid&gt;，不实现任何多租户/分表接口，
/// 从而完全免租户上下文（Repository.CompareTenantId 对非 IIncludeMultiTenant 实体直接放行）。
/// </summary>
[Table("Products")]
public class Product : AggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
