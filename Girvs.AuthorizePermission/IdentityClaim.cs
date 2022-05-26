using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Newtonsoft.Json;

namespace Girvs.AuthorizePermission
{
    /// <summary>
    /// 身份标识描述实体类
    /// </summary>
    public class IdentityClaimManager
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
        public UserType UserType { get; set; }
        public IdentityType IdentityType { get; set; }
        public SystemModule FuncModule { get; set; }

        public string ToJsonStr()
        {
            return JsonConvert.SerializeObject(this);
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>()
            {
                {ClaimTypes.Sid, UserId.ToString()},
                {ClaimTypes.Name, UserName},
                {ClaimTypes.GroupSid, TenantId.ToString()},
                {ClaimTypes.GivenName, TenantName},
                {ClaimTypes.NameIdentifier, UserType.ToString()},
                {ClaimTypes.Locality, IdentityType.ToString()},
                {ClaimTypes.System, FuncModule.ToString()}
            };
        }

        public ClaimsIdentity ToClaimsIdentity()
        {
            return new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Sid, UserId.ToString()),
                new Claim(ClaimTypes.Name, UserName),
                new Claim(ClaimTypes.GroupSid, TenantId.ToString()),
                new Claim(ClaimTypes.GivenName, TenantName),
                new Claim(ClaimTypes.NameIdentifier, UserType.ToString()),
                new Claim(ClaimTypes.Locality, IdentityType.ToString()),
                new Claim(ClaimTypes.System, FuncModule.ToString()),
            });
        }

        public static IdentityClaimManager FromHttpRequestToken()
        {
            var httpContext = EngineContext.Current.HttpContext;
            if (httpContext?.User.Identity?.IsAuthenticated == true)
            {
                var claims = httpContext.User.Claims.ToDictionary(x => x.Type, v => v.Value);
                var identityClaim = new IdentityClaimManager()
                {
                    UserId = claims[ClaimTypes.Sid].ToGuidDefaultEmpty(),
                    UserName = claims[ClaimTypes.Name],
                    IdentityType = claims[ClaimTypes.Locality].ToEnum<IdentityType>()
                };

                if (identityClaim.IdentityType == IdentityType.RegisterUser)
                {
                    var tenantId = httpContext.Request.Headers["TenantId"];
                    identityClaim.TenantId = tenantId.ToString().ToGuidDefaultEmpty();
                    identityClaim.TenantName = httpContext.Request.Headers["TenantName"];
                }
                else
                {
                    identityClaim.TenantId = claims[ClaimTypes.GroupSid].ToGuidDefaultEmpty();
                    identityClaim.TenantName = claims[ClaimTypes.GivenName];
                    identityClaim.UserType = claims[ClaimTypes.NameIdentifier].ToEnum<UserType>();
                    identityClaim.FuncModule = claims[ClaimTypes.System].ToEnum<SystemModule>();
                }

                return identityClaim;
            }

            return null;
        }

        public static IdentityClaimManager FromCapHeader(IDictionary<string, string> capHeader)
        {
            return new IdentityClaimManager
            {
                UserId = Guid.Parse(capHeader[ClaimTypes.Sid]),
                UserName = capHeader[ClaimTypes.Name],
                TenantId = Guid.Parse(capHeader[ClaimTypes.GroupSid]),
                TenantName = capHeader[ClaimTypes.GivenName],
                UserType = (UserType) Convert.ToInt32(capHeader[ClaimTypes.NameIdentifier]),
                IdentityType = (IdentityType) Convert.ToInt32(capHeader[ClaimTypes.Locality]),
                FuncModule = (SystemModule) Convert.ToInt32(capHeader[ClaimTypes.System])
            };
        }
    }
}