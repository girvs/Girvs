using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Role;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IRoleAppService : IAppWebApiService
    {
        Task<RoleDetailViewModel> GetAsync(Guid id);
        Task<RoleEditViewModel> CreateAsync(RoleEditViewModel model);
        Task<RoleEditViewModel> UpdateAsync(Guid id, RoleEditViewModel model);
        Task DeleteAsync(Guid id);
        Task<List<RoleListViewModel>> GetAsync();
    }
}