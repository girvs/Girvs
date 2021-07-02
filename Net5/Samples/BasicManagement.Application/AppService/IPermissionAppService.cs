using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.Permission;
using Girvs.DynamicWebApi;

namespace BasicManagement.Application.AppService
{
    public interface IPermissionAppService : IAppWebApiService
    {
        Task<List<PermissionByCurrentUserViewModel>> Get();
        Task<List<string>> Get(Guid appliedObjectId);
        Task<List<PermissionDetailViewModel>> Get(PermissionQueryViewModel queryViewModel);
        Task Update(SavePermisssionEditViewModel saveViewModels);
    }
}