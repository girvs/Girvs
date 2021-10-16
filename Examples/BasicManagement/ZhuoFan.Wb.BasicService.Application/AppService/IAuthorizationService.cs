using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.AuthorizePermission;
using Girvs.BusinessBasis;
using Girvs.DynamicWebApi;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IAuthorizationService : IAppWebApiService, IManager
    {
        Task<IList<AuthorizePermissionModel>> GetFunctionOperateList();
        Task<IList<AuthorizeDataRuleModel>> GetDataRuleList();
        Task InitAuthorization();
    }
}