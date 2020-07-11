using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Girvs.Domain.Managers
{
    public class ClaimManager : IClaimManager
    {
        public async Task<List<Claim>> CreateClaims(string sid, string name, string tenantId,
            params KeyValuePair<string, string>[] other)
        {
            return await Task.Run(() =>
            {
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Sid, sid),
                    new Claim(ClaimTypes.Name, name),
                    new Claim("TenantId", tenantId),
                };

                foreach (var dictionary in other)
                {
                    claims.Add(new Claim(
                        dictionary.Key, dictionary.Value
                    ));
                }

                return claims;
            });
        }
    }
}