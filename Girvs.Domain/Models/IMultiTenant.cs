using System;

namespace Girvs.Domain.Models
{
    public interface IMultiTenant
    {
        Guid TenantId { get; set; }
    }
}