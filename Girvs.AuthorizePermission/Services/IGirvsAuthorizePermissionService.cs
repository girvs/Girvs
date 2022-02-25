using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.BusinessBasis;
using Girvs.DynamicWebApi;

namespace Girvs.AuthorizePermission.Services
{
    public interface IGirvsAuthorizePermissionService : IAppWebApiService, IManager
    {
        Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();

        Task<List<AuthorizeDataRuleModel>> GetAuthorizeDataRuleList();
    }
}