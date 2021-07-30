using System.Security.Claims;
using Girvs.BusinessBasis;

namespace Girvs.Infrastructure
{
    public interface IClaimManager : IManager
    {
        string GetUserId(string claimName = ClaimTypes.Sid);
        string GetUserName(string claimName = ClaimTypes.Name);
        string GetTenantId(string claimName = ClaimTypes.GroupSid);
        string GetTenantName(string claimName = ClaimTypes.GivenName);

        ClaimsIdentity GenerateClaimsIdentity(string sid, string name, string tenantId,
            string tenantName);
    }
}