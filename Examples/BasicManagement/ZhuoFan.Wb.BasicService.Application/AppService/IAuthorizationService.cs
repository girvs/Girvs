using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Application.AppService
{
    public interface IAuthorizationService : IAppWebApiService
    {
        Task<IList<ServicePermission>> GetFunctionOperateList();
        Task<IList<ServiceDataRule>> GetDataRuleList();

        Task InitAuthorization();
    }
}