using System;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Application.ViewModels.User;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IUserAppService : IAppWebApiService
    {
        Task<UserDetailViewModel> GetAsync(Guid id);
        Task<UserEditViewModel> CreateAsync(UserEditViewModel model);
        Task<UserEditViewModel> UpdateAsync(Guid id, UserEditViewModel model);
        Task DeleteAsync(Guid id);
        Task<UserQueryViewModel> GetAsync(UserQueryViewModel model);
        // Task<UserDetailViewModel> GetByAccount(string account);
        Task<string> GetToken(string account, string password);
    }
}