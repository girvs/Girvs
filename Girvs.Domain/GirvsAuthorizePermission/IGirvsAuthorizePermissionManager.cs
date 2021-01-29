using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Managers;

namespace Girvs.Domain.GirvsAuthorizePermission
{
    public interface IGirvsAuthorizePermissionManager : IManager
    {
        Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();
    }
}