namespace Girvs.Domain.Models
{
    public interface IIncludeMultiTenant<TTenantKey>
    {
        public TTenantKey TenantId { get; set; }
    }
}