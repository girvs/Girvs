using System;
using System.Collections.Generic;
using BasicManagement.Domain.Enumerations;
using Girvs.BusinessBasis.Entities;

namespace BasicManagement.Domain.Models
{
    public class User  : AggregateRoot<Guid>, IIncludeInitField
    {
        public User()
        {
            Roles = new List<Role>();
        }

        public Guid Id { get; set; }
        
        /// <summary>
        /// 登陆名称
        /// </summary>
        public string UserAccount { get; set; }

        /// <summary>
        /// 用户密码
        /// </summary>
        public string UserPassword { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 用户联系方式
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// 绑定其它相关服务的关键标识Id
        /// </summary>
        public Guid OtherId { get; set; }

        /// <summary>
        /// 用户的状态
        /// </summary>
        public DataState State { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }

        /// <summary>
        /// 是否初始化数据
        /// </summary>
        public bool IsInitData { get; set; }
        
        public virtual List<Role> Roles { get; set; }
    }
}