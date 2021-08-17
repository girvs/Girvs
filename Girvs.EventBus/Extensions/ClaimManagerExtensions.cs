﻿using System.Security.Claims;
using DotNetCore.CAP;
using Girvs.Infrastructure;

namespace Girvs.EventBus.Extensions
{
    public static class ClaimManagerExtensions
    {
        public static void CapEventBusReSetClaim(this IClaimManager claimManager, CapHeader capHeader)
        {
            var userId = capHeader.ContainsKey(ClaimTypes.Sid) ? capHeader[ClaimTypes.Sid] : string.Empty;
            var userName = capHeader.ContainsKey(ClaimTypes.Name) ? capHeader[ClaimTypes.Name] : string.Empty;
            var tenantId = capHeader.ContainsKey(ClaimTypes.GroupSid) ? capHeader[ClaimTypes.GroupSid] : string.Empty;

            var tenantName = capHeader.ContainsKey(ClaimTypes.GivenName)
                ? capHeader[ClaimTypes.GivenName]
                : string.Empty;
            // var identityTypeStr = capHeader.ContainsKey(ClaimTypes.NameIdentifier)
            //     ? capHeader[ClaimTypes.NameIdentifier]
            //     : string.Empty;
            //
            // var identityType = (IdentityType)System.Enum.Parse(typeof(IdentityType), identityTypeStr);

            claimManager.CurrentClaims =
                claimManager
                    .GenerateClaimsIdentity(userId, userName, tenantId, tenantName, IdentityType.EventMessageUser)
                    .Claims;
        }
    }
}