using System;
using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.User;
using Girvs.DynamicWebApi;

namespace BasicManagement.Application.AppService
{
    public interface IUserAppService : IAppWebApiService
    {
        Task<UserDetailViewModel> GetAsync(Guid id);
        Task<UserEditViewModel> CreateAsync(UserEditViewModel model);
        Task<UserEditViewModel> UpdateAsync(UserEditViewModel model);
        Task DeleteAsync(Guid id);
        Task<UserQueryViewModel> GetAsync(UserQueryViewModel model);
        Task<UserDetailViewModel> GetByAccount(string account);

        Task<UserDetailViewModel> GetByOtherId(Guid otherId);

        Task<string> Test();
    }
}