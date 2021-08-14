using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;

namespace Girvs.AuthorizePermission.ActionPermission
{
    public abstract class GirvsServiceMethodPermissionCompare : IServiceMethodPermissionCompare
    {
        public abstract Task<IEnumerable<AuthorizePermissionModel>> GetFunctionPermissions();

        public async Task<bool> PermissionCompare(Guid functionId, Permission permission)
        {
            var ps = await GetFunctionPermissions();
            var key = permission.ToString();
            var functionPermission = ps.First(x => x.ServiceId == functionId);
            
            
            return functionPermission.Permissions.ContainsKey(key);
        }
    }
}