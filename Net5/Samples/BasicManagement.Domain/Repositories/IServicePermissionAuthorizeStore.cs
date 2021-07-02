using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.BusinessBasis;

namespace BasicManagement.Domain.Repositories
{
    public interface IServicePermissionAuthorizeStore : IManager
    {
        Task<List<AuthorizePermissionModel>> GetList();
        Task CreateOrUpdate(List<AuthorizePermissionModel> list);
    }
}