using System.Collections.Generic;

namespace Girvs.AuthorizePermission
{
    public class AuthorizeModel
    {
        public AuthorizeModel()
        {
            AuthorizeDataRules = new List<AuthorizeDataRuleModel>();
            AuthorizePermissions = new List<AuthorizePermissionModel>();
        }
        public List<AuthorizeDataRuleModel> AuthorizeDataRules { get; set; }
        
        public List<AuthorizePermissionModel> AuthorizePermissions { get; set; }
    }
}