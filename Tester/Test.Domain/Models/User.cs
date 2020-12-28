using System;
using System.Collections.Generic;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;
using Test.Domain.Enumerations;

namespace Test.Domain.Models
{
    public class User : AggregateRoot<Guid>, IIncludeMultiTenant<Guid>, IIncludeCreateTime
    {
        public User()
        {
            UserRoles = new List<UserRole>();
        }

        ///<summary></summary>
        public string UserAccount { get; set; }

        ///<summary></summary>
        public string UserPassword { get; set; }

        ///<summary></summary>
        public string UserName { get; set; }

        public string ContactNumber { get; set; }

        public DataState State { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }

        public virtual List<UserRole> UserRoles { get; set; }
        public Guid TenantId { get; set; } = EngineContext.Current.CurrentClaimTenantId;
        public DateTime CreateTime { get; set; }
    }
}