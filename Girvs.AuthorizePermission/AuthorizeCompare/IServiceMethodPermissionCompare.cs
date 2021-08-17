using System;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis;

namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public interface IServiceMethodPermissionCompare: IManager
    {
        bool PermissionCompare(Guid functionId, Permission permission);
    }
}