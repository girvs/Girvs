using System.Linq;
using DotNetCore.CAP;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.EventBus.Extensions
{
    public static class ClaimManagerExtensions
    {
        public static void CapEventBusReSetClaim(this IGirvsClaimManager claimManager, CapHeader capHeader)
        {
            var claimdic = capHeader.ToDictionary(x => x.Key, v => v.Value);
            claimdic.SetDictionaryKeyValue(GirvsIdentityClaimTypes.IdentityType,
                IdentityType.EventMessageUser.ToString());
            claimManager.SetFromDictionary(claimdic);
        }
    }
}