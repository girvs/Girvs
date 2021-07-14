using System.Security.Claims;

namespace Girvs.AuthorizePermission.Configuration
{
    public class ClaimValueConfig
    {
        /// <summary>
        /// 当前用户Id
        /// </summary>
        public string ClaimSid { get; set; } = ClaimTypes.Sid;
        
        /// <summary>
        /// 当前用户名称
        /// </summary>
        public string ClaimName { get; set; } = ClaimTypes.Name;

        /// <summary>
        /// 当前租户Id
        /// </summary>
        public string ClaimTenantId { get; set; } = ClaimTypes.GroupSid;
        
        /// <summary>
        /// 当前租户名称
        /// </summary>
        public string ClaimTenantName { get; set; } = ClaimTypes.GivenName;
    }
}