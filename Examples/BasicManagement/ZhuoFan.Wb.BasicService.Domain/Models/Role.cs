using System;
using System.Collections.Generic;
using Girvs.BusinessBasis.Entities;

namespace ZhuoFan.Wb.BasicService.Domain.Models
{
    public class Role : AggregateRoot<Guid>, IIncludeInitField,IIncludeMultiTenant<Guid>
    {
        public Role()
        {
            Users = new List<User>();
        }
        
        public string Name { get; set; }
        public string Desc { get; set; }
        public virtual List<User> Users { get; set; }
        public bool IsInitData { get; set; }
        public Guid TenantId { get; set; }
    }
}
