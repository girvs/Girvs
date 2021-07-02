using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Domain.GirvsAuthorizePermission
{
    public interface IGirvsAuthorizePermissionManager : IManager
    {
        Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();
    }
}