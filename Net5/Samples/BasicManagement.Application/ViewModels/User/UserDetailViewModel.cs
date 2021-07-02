using System;
using System.Collections.Generic;
using BasicManagement.Application.ViewModels.Role;
using BasicManagement.Domain.Enumerations;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;

namespace BasicManagement.Application.ViewModels.User
{
    [AutoMapFrom(typeof(Domain.Models.User))]
    public class UserDetailViewModel : IDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户登陆名称
        /// </summary>
        public string UserAccount { get; set; }

        /// <summary>
        /// 用户登陆密码
        /// </summary>
        public string UserPassword { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public DataState State { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }

        /// <summary>
        /// 绑定其它相关服务的关键标识Id
        /// </summary>
        public Guid OtherId { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public List<RoleDetailViewModel> UserRoles { get; set; }
    }
}