using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;

namespace Girvs.AuthorizePermission.Services
{
    public interface IGirvsAuthorizePermissionService : IAppWebApiService
    {
        Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();

        Task<List<AuthorizeDataRuleModel>> GetAuthorizeDataRuleList();
    }
}