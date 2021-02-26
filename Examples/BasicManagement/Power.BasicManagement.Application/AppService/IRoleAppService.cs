using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Application;
using Power.BasicManagement.Application.ViewModels.Role;

namespace Power.BasicManagement.Application.AppService
{
    public interface IRoleAppService : IAppWebApiService
    {
        Task<RoleDetailViewModel> GetAsync(Guid id);
        Task<RoleEditViewModel> CreateAsync(RoleEditViewModel model);
        Task<RoleEditViewModel> UpdateAsync(RoleEditViewModel model);
        Task DeleteAsync(Guid id);
        Task<List<RoleListViewModel>> GetAsync();
    }
}