using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Repositories
{
    public interface IPermissionRepository : IRepository<BasalPermission>
    {
        /// <summary>
        /// 获取用户所授权限
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>权限列表</returns>
        Task<List<BasalPermission>> GetUserPermissionLimit(Guid userId);


        /// <summary>
        /// 获取角色所授权限
        /// </summary>
        /// <param name="roleId">角色Id</param>
        /// <returns>权限列表</returns>
        Task<List<BasalPermission>> GetRolePermissionLimit(Guid roleId);


        /// <summary>
        /// 获取角色列表所授权限
        /// </summary>
        /// <param name="roleIds">角色Ids</param>
        /// <returns>权限列表</returns>
        Task<List<BasalPermission>> GetRoleListPermissionLimit(Guid[] roleIds);

        /// <summary>
        /// 保存权限
        /// </summary>
        /// <param name="ps">权限集合</param>
        /// <returns>是否成功</returns>
        Task UpdatePermissions(List<BasalPermission> ps);
    }
}