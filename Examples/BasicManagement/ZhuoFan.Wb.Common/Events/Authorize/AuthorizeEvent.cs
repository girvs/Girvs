using System.Collections.Generic;

namespace ZhuoFan.Wb.Common.Events.Authorize
{
    public record AuthorizeEvent: IntegrationEvent
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