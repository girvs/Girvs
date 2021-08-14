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

        public Task<bool> PermissionCompare(Guid functionId, Permission permission)
        {
            var ps = GetFunctionPermissions().Result;

            if (ps == null || !ps.Any())
            {
                throw new GirvsException("未获取相关的功能授权信息", 568);
            }

            var key = permission.ToString();
            var result = ps.Any(x => x.ServiceId == functionId && x.Permissions.ContainsKey(key));
            return Task.FromResult(result);
        }
    }
}