using System.Collections.Generic;

namespace Girvs.AuthorizePermission
{
    public class AuthorizeModel
    {
        public List<AuthorizeDataRuleModel> AuthorizeDataRules { get; set; }
        
        public List<AuthorizePermissionModel> AuthorizePermissions { get; set; }
    }
}