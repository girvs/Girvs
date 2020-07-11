using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Girvs.Domain.Managers
{
    public interface IClaimManager : IManager
    {
        Task<List<Claim>> CreateClaims(string sid, string name, string tenantId,
            params KeyValuePair<string, string>[] other);
    }
}