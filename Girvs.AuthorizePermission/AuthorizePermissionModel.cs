using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace Girvs.AuthorizePermission
{
    public class AuthorizePermissionModel : IDto
    {
        public AuthorizePermissionModel()
        {
            Permissions = new Dictionary<string, string>();
            OperationPermissionModels = new List<OperationPermissionModel>();
        }

        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }

        public List<OperationPermissionModel> OperationPermissionModels { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }


    public class OperationPermissionModel : IDto
    {
        public string OperationName { get; set; }
        public Permission Permission { get; set; }
        public UserType UserType { get; set; }
    }
}