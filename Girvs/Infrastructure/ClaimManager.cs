using System.Security.Claims;

namespace Girvs.Infrastructure
{
    public class ClaimManager : IClaimManager
    {
        public string GetUserId(string claimName = ClaimTypes.Sid)
        {
            var userId = EngineContext.Current.GetCurrentClaimByName(claimName).Value;
            return string.IsNullOrWhiteSpace(userId) ? string.Empty : userId;
        }

        public string GetUserName(string claimName = ClaimTypes.Name)
        {
            var userName = EngineContext.Current.GetCurrentClaimByName(claimName).Value;
            return string.IsNullOrWhiteSpace(userName) ? string.Empty : userName;
        }

        public string GetTenantId(string claimName = ClaimTypes.GroupSid)
        {
            var tenantId = EngineContext.Current.GetCurrentClaimByName(claimName).Value;
            return string.IsNullOrWhiteSpace(tenantId) ? string.Empty : tenantId;
        }

        public string GetTenantName(string claimName = ClaimTypes.GivenName)
        {
            var tenantName = EngineContext.Current.GetCurrentClaimByName(claimName).Value;
            return string.IsNullOrWhiteSpace(tenantName) ? string.Empty : tenantName;
        }

        public ClaimsIdentity GenerateClaimsIdentity(string sid, string name, string tenantId, string tenantName)
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Sid, sid),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.GroupSid, tenantId),
                new Claim(ClaimTypes.GivenName, tenantName)
            });

            return claimsIdentity;
        }
    }
}