namespace Girvs.BusinessBasis.Entities
{
    public interface IIncludeMultiTenant<TTenantKey>
    {
        public TTenantKey TenantId { get; set; }
    }
}