using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;

namespace Girvs.AuthorizePermission.ActionPermission
{
    public interface IGirvsAuthorizePermissionService : IAppWebApiService
    {
        Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();
    }
}