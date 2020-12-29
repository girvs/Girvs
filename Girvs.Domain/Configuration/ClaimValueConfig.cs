using System.Security.Claims;

namespace Girvs.Domain.Configuration
{
    public class ClaimValueConfig
    {
        public string ClaimSid { get; set; } = ClaimTypes.Sid;
        public string ClaimName { get; set; } = ClaimTypes.Name;

        public string ClaimTenantId { get; set; } = ClaimTypes.GroupSid;
        public string ClaimTenantName { get; set; } = ClaimTypes.GivenName;
    }
}