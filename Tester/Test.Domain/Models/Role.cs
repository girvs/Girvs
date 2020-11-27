using System;
using System.Collections.Generic;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;

namespace Test.Domain.Models
{ 
    public class Role : AggregateRoot,IMultiTenant
    {
        public Role()
        {
            UserRoles = new List<UserRole>();
        }
        public string Name { get; set; }

        public string Desc { get; set; }
        public virtual List<UserRole> UserRoles { get; set; }
        public Guid TenantId { get; set; } = EngineContext.Current.CurrentClaimTenantId;
    }
}
