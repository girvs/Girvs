using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.AuthorizePermission;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Role;
using ZhuoFan.Wb.BasicService.Application.ViewModels.User;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IRoleAppService : IAppWebApiService
    {
        Task<RoleDetailViewModel> GetAsync(Guid id);
        Task<RoleEditViewModel> CreateAsync(RoleEditViewModel model);
        Task<RoleEditViewModel> UpdateAsync(Guid id, RoleEditViewModel model);
        Task DeleteAsync(Guid id);
        Task DeleteAsync(IList<Guid> ids);
        Task<List<RoleListViewModel>> GetAsync();
        Task AddRoleUser(Guid roleId,EditRoleUserViewModel model);
        Task DeleteRoleUser(Guid roleId,EditRoleUserViewModel model);
        Task<List<UserQueryListViewModel>> GetRoleUsers(Guid roleId);
        
        /// <summary>
        /// 获取指定角色的权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        Task<List<AuthorizePermissionModel>> GetRolePermission(Guid roleId);

        /// <summary>
        /// 保存指定角色的权限
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        Task SaveRolePermission(Guid roleId, List<AuthorizePermissionModel> models);
    }
}