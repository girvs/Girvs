using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.Role;
using Girvs.DynamicWebApi;

namespace BasicManagement.Application.AppService
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