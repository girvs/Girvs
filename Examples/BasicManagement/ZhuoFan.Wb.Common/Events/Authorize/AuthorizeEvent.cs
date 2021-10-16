using System.Collections.Generic;
using Girvs.AuthorizePermission;
using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Authorize
{
    public class AuthorizeEvent: IntegrationEvent
    {
        public AuthorizeEvent()
        {
            AuthorizePermissions = new List<AuthorizePermissionModel>();
            AuthorizeDataRules = new List<AuthorizeDataRuleModel>();
        }
        
        public List<AuthorizeDataRuleModel> AuthorizeDataRules { get; set; }
        
        public List<AuthorizePermissionModel> AuthorizePermissions { get; set; }
    }
}