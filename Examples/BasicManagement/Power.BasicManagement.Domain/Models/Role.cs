using System;
using System.Collections.Generic;
using Girvs.Domain.Models;

namespace Power.BasicManagement.Domain.Models
{
    public class Role : AggregateRoot<Guid>, IIncludeInitField
    {
        public Role()
        {
            UserRoles = new List<UserRole>();
        }
        public string Name { get; set; }

        public string Desc { get; set; }
        public virtual List<UserRole> UserRoles { get; set; }
        public bool IsInitData { get; set; }
    }
}
