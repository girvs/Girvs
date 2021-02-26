using Girvs.Application.Mapper;
using System;

namespace Power.BasicManagement.Application.ViewModels.User
{
    /// <summary>
    /// 用户角色
    /// </summary>
    [AutoMapFrom(typeof(Domain.Models.UserRole))]
    public class UserRoleDto
    {
        /// <summary>
        /// 用户Id
        /// </summary>
        public Guid UserId { get; set; }
        /// <summary>
        /// 角色Id
        /// </summary>
        public Guid RoleId { get; set; }
    }
}
