namespace Girvs.Domian.Entities
{
    public interface IIncludeMultiTenant<TTenantKey>
    {
        public TTenantKey TenantId { get; set; }
    }
}