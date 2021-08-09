using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Permission;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IPermissionAppService : IAppWebApiService
    {
        Task<List<PermissionByCurrentUserViewModel>> Get();
        Task<List<string>> Get(Guid appliedObjectId);
        Task<List<PermissionDetailViewModel>> Get(PermissionQueryViewModel queryViewModel);
        Task Update(SavePermisssionEditViewModel saveViewModels);
    }
}