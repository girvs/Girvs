using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.GirvsAuthorizePermission;
using Girvs.Domain.Managers;

namespace Power.BasicManagement.Domain.Repositories
{
    public interface IServicePermissionAuthorizeStore : IManager
    {
        Task<List<AuthorizePermissionModel>> GetList();
        Task CreateOrUpdate(List<AuthorizePermissionModel> list);
    }
}