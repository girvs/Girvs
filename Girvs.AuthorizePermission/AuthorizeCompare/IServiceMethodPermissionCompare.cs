using System;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis;

namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public interface IServiceMethodPermissionCompare : IManager
    {
        Task<bool> PermissionCompare(Guid functionId, Permission permission);
    }
}