using System;
using Girvs.Configuration;

namespace Girvs.AuthorizePermission.Configuration
{
    public class AuthorizeConfig : IAppModuleConfig
    {
        public ClaimValueConfig ClaimValueConfig { get; set; } = new ClaimValueConfig();
        public AuthorizationModel AuthorizationModel { get; set; } = AuthorizationModel.Jwt;
        public IdentityServer4Config IdentityServer4Config { get; set; } = new IdentityServer4Config();
        public JwtConfig JwtConfig { get; set; } = new JwtConfig();

        public bool UseServiceMethodPermissionCompare { get; set; } = true;

        public void Init()
        {
        }
    }

    public class JwtConfig
    {
        /// <summary>
        /// Secret
        /// </summary>
        public string Secret { get; set; } = "Girvs_Secret_168168";

        /// <summary>
        /// 过期时间（小时）
        /// </summary>
        public int ExpiresHours { get; set; } = 1;
    }

    public class IdentityServer4Config
    {
        public string ServerHost { get; set; } = "http://localhost:5001";
        public string ClientName { get; set; } = "ApiName1";
        public bool UseHttps { get; set; } = false;
        public string ApiSecret { get; set; } = "ApiSecret";
    }

    [Flags()]
    public enum AuthorizationModel : long
    {
        Jwt = 1,
        IdentityServer4 = 2,
        JwtAndIdentityServer4 = 3
    }
}