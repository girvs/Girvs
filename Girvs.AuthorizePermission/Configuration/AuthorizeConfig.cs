using Girvs.Configuration;

namespace Girvs.AuthorizePermission.Configuration;

public class AuthorizeConfig : IAppModuleConfig
{
    public AuthorizationModel AuthorizationModel { get; set; } = AuthorizationModel.Jwt;
    public OAuth2Config OAuth2Config { get; set; } = new OAuth2Config();
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

public class OAuth2Config
{
    public string Authority { get; set; } = "http://localhost:5001";
    public string Audience { get; set; } = AppDomain.CurrentDomain.FriendlyName
        .Replace(".", "_");
    public bool RequireHttpsMetadata { get; set; } = false;

    public bool ValidateIssuerSigningKey { get; set; } = false;
    public bool ValidateIssuer { get; set; } = false;
    public bool ValidateAudience { get; set; } = true;
}

[Flags()]
public enum AuthorizationModel : long
{
    Jwt = 1,
    OAuth2 = 2,
    JwtWebFront = 4
}
