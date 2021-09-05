using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Girvs.Infrastructure
{
    public class ClaimManager : IClaimManager
    {
        public IEnumerable<Claim> CurrentClaims { get; set; } = new List<Claim>();

        private string GetClaimValueByTypeName(string claimName)
        {
            var claim = CurrentClaims?.FirstOrDefault(x => x.Type == claimName);
            return claim?.Value ?? string.Empty;
        }

        public string GetUserId(string claimName = ClaimTypes.Sid)
        {
            return GetClaimValueByTypeName(claimName);
        }

        public string GetUserName(string claimName = ClaimTypes.Name)
        {
            return GetClaimValueByTypeName(claimName);
        }

        public string GetTenantId(string claimName = ClaimTypes.GroupSid)
        {
            return GetClaimValueByTypeName(claimName);
        }

        public string GetTenantName(string claimName = ClaimTypes.GivenName)
        {
            return GetClaimValueByTypeName(claimName);
        }

        public IdentityType GetIdentityType(string claimName = ClaimTypes.NameIdentifier)
        {
            var identityTypeStr = GetClaimValueByTypeName(claimName);
            return (IdentityType)System.Enum.Parse(typeof(IdentityType), identityTypeStr);
        }

        public ClaimsIdentity GenerateClaimsIdentity(string sid, string name, string tenantId, string tenantName,
            IdentityType identityType = IdentityType.ManagerUser)
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Sid, sid),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.GroupSid, tenantId ?? string.Empty),
                new Claim(ClaimTypes.GivenName, tenantName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, identityType.ToString())
            });

            return claimsIdentity;
        }
    }
}
