using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.AuthorizePermission;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Role;
using ZhuoFan.Wb.BasicService.Application.ViewModels.User;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IUserAppService : IAppWebApiService
    {
        Task<UserDetailViewModel> GetAsync(Guid id);
        Task<UserEditViewModel> CreateAsync(UserEditViewModel model);
        Task<UserEditViewModel> UpdateAsync(Guid id, UserEditViewModel model);
        Task DeleteAsync(Guid id);
        Task DeleteAsync(IList<Guid> ids);
        Task<UserQueryViewModel> GetAsync(UserQueryViewModel model);
        // Task<string> GetToken(string account, string password);
        Task<string> GetToken(UserLoginViewModel model);
        Task AddUserRole(Guid userId, EditUserRoleViewModel model);
        Task DeleteUserRole(Guid userId, EditUserRoleViewModel model);
        Task Enable(Guid userId);
        Task Disable(Guid userId);
        Task ChangeUserPassword(Guid userId, ChangeUserPassworkViewModel model);
        Task UserEditPassword(UserEditPasswordViewModel model);
        Task<List<RoleListViewModel>> GetUserRoles(Guid userId);
        Task<bool> ExistAccount(string account);

        Task BatchReSetUserPassword(BatchResetUserPasswordViewModel model);
        Task BatchChangeUserState(BatchChangeUserStateViewModel model);
        
        /// <summary>
        /// 获取当前用户的权限、包含功能菜单以及数据权限
        /// </summary>
        /// <returns></returns>
        Task<AuthorizeModel> GetCurrentUserAuthorization();
        
        /// <summary>
        /// 获取当前用户的权限、包含功能菜单以及数据权限
        /// </summary>
        /// <returns></returns>
        Task<AuthorizeModel> GetCurrentUserAuthorization(Guid userId);

        /// <summary>
        /// 获取指定用户的数据权限
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<UserDataRuleListViewModel>> GetUserDataRule(Guid userId);
        
        /// <summary>
        /// 保存指定用户的数据授权
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        Task SaveUserDataRule(Guid userId, List<SaveUserDataRuleViewModel> models);
    }
}