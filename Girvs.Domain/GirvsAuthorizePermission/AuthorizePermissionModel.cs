using System;
using System.Collections.Generic;

namespace Girvs.Domain.GirvsAuthorizePermission
{
    public class AuthorizePermissionModel
    {
        public AuthorizePermissionModel()
        {
            Permissions = new Dictionary<string, string>();
        }

        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }
}