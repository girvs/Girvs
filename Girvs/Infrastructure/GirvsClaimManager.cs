using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Girvs.Extensions;

namespace Girvs.Infrastructure
{
    public class GirvsClaimManager : IGirvsClaimManager
    {
        public string GetTenantId() => IdentityClaim.TenantId;

        public string GetUserId() => IdentityClaim.UserId;

        public string GetUserName() => IdentityClaim.UserName;

        public string GetTenantName() => IdentityClaim.TenantName;

        public IdentityType GetIdentityType() => IdentityClaim.IdentityType;

        public GirvsIdentityClaim IdentityClaim { get; set; }

        public void SetFromHttpRequestToken()
        {
            var httpContext = EngineContext.Current.HttpContext;
            if (httpContext?.User.Identity?.IsAuthenticated == true)
            {
                var claims = httpContext.User.Claims.ToDictionary(x => x.Type, v => v.Value);
                var identityType = claims.GetDictionaryValueByKey(GirvsIdentityClaimTypes.IdentityType)
                    .ToEnum<IdentityType>();

                if (identityType == IdentityType.RegisterUser)
                {
                    var tenantId = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantId)];
                    var tenantName = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantName)];

                    claims.Add(GirvsIdentityClaimTypes.TenantId, tenantId);
                    claims.Add(GirvsIdentityClaimTypes.TenantName, HttpUtility.UrlDecode(tenantName));
                }

                SetFromDictionary(claims);
            }
        }

        public void SetFromDictionary(Dictionary<string, string> dictionary)
        {
            IdentityClaim = new GirvsIdentityClaim
            {
                OtherClaims = dictionary
            };

            IdentityClaim.UserId =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.UserId);

            IdentityClaim.UserName =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.UserName);

            IdentityClaim.TenantId =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.TenantId);

            IdentityClaim.TenantName =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.TenantName);

            IdentityClaim.IdentityType =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.IdentityType).ToEnum<IdentityType>();

            IdentityClaim.SystemModule =
                dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.ClaimSystemModule)
                    .ToEnum<SystemModule>();
        }

        public ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim)
        {
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.UserId,
                girvsIdentityClaim.UserId);
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.UserName,
                girvsIdentityClaim.UserName);
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantId,
                girvsIdentityClaim.TenantId);
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantName,
                girvsIdentityClaim.TenantName);
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.IdentityType,
                girvsIdentityClaim.IdentityType.ToString());
            girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.ClaimSystemModule,
                girvsIdentityClaim.SystemModule.ToString());

            var claims = girvsIdentityClaim.OtherClaims.Select(x => new Claim(
                x.Key, x.Value));

            return new ClaimsIdentity(claims);
        }
    }
}