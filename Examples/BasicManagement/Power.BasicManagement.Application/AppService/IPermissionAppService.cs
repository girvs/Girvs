using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Application;
using Power.BasicManagement.Application.ViewModels.Permission;

namespace Power.BasicManagement.Application.AppService
{
    public interface IPermissionAppService : IAppWebApiService
    {
        Task<List<PermissionByCurrentUserViewModel>> Get();
        Task<List<string>> Get(Guid appliedObjectId);
        Task<List<PermissionDetailViewModel>> Get(PermissionQueryViewModel queryViewModel);
        Task Update(SavePermisssionEditViewModel saveViewModels);
    }
}