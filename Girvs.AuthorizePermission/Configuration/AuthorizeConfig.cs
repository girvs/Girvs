using Girvs.Configuration;

namespace Girvs.AuthorizePermission.Configuration
{
    public class AuthorizeConfig : IAppModuleConfig
    {
        public AuthorizationModel AuthorizationModel { get; set; } = AuthorizationModel.Jwt;
        public IdentityServer4Config IdentityServer4Config { get; set; } = new IdentityServer4Config();
        public JwtConfig JwtConfig { get; set; } = new JwtConfig();
        
        public JwtConfig JwtWebFrontConfig { get; set; } = new JwtConfig();

        public bool UserDataRuleDefaultAll { get; set; } = true;

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
        public string ApiResourceName { get; set; } = AppDomain.CurrentDomain.FriendlyName
            .Replace(".", "_");
        public bool UseHttps { get; set; } = false;
        public string ApiSecret { get; set; } = "zhuofan@ids4";

        public bool ValidateIssuerSigningKey { get; set; } = false;
        public bool ValidateIssuer { get; set; } = false;
        public bool ValidateAudience { get; set; } = true;
    }

    [Flags()]
    public enum AuthorizationModel : long
    {
        Jwt = 1,
        IdentityServer4 = 2,
        JwtWebFront = 4
    }
}
