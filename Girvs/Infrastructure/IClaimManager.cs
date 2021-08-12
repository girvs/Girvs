using System.Collections.Generic;
using System.Security.Claims;
using Girvs.BusinessBasis;

namespace Girvs.Infrastructure
{
    public interface IClaimManager : IManager
    {
        IEnumerable<Claim> CurrentClaims { get; set; }
        string GetUserId(string claimName = ClaimTypes.Sid);
        string GetUserName(string claimName = ClaimTypes.Name);
        string GetTenantId(string claimName = ClaimTypes.GroupSid);
        string GetTenantName(string claimName = ClaimTypes.GivenName);
        IdentityType GetIdentityType(string claimName = ClaimTypes.NameIdentifier);

        ClaimsIdentity GenerateClaimsIdentity(string sid, string name, string tenantId,
            string tenantName, IdentityType identityType = IdentityType.ManagerUser);
    }


    /// <summary>
    /// 登陆身份类型
    /// </summary>
    public enum IdentityType
    {
        /// <summary>
        /// 后台管理用户
        /// </summary>
        ManagerUser,

        /// <summary>
        /// 前台注册用户
        /// </summary>
        RegisterUser,

        /// <summary>
        /// 事件触发用户
        /// </summary>
        EventMessageUser
    }
}