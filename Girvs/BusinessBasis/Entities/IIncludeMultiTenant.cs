namespace Girvs.BusinessBasis.Entities;

public interface IIncludeMultiTenant<TTenantKey>
{
    TTenantKey TenantId { get; set; }
}