using System;
using System.Collections.Generic;
using Girvs.BusinessBasis.Dto;

namespace Girvs.AuthorizePermission
{
    public class AuthorizePermissionModel : IDto
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