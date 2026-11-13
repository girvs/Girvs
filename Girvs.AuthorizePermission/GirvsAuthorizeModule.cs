using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Girvs.AuthorizePermission.Middleware;

namespace Girvs.AuthorizePermission;

public class GirvsAuthorizeModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

        // 默认 Scheme 必须指向下面真正注册的处理器之一。原先固定为 "Bearer"，
        // 但并未注册名为 Bearer 的处理器：AuthenticationMiddleware 会静默跳过，
        // HttpContext.User 永远匿名，使用默认策略的 [Authorize] 则在认证时抛异常。
        var defaultScheme = ResolveDefaultAuthenticationScheme(authorizeConfig.AuthorizationModel);
        var authenticationBuilder = services.AddAuthentication(defaultScheme);

        if ((authorizeConfig.AuthorizationModel & AuthorizationModel.Jwt) == AuthorizationModel.Jwt)
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsJwt,
                x =>
                {
                    //使用应用密钥得到一个加密密钥字节数组
                    var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtConfig.Secret);
                    x.RequireHttpsMetadata = true;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    x.Events = new JwtBearerEvents();
                }
            );
        }

        if (
            (authorizeConfig.AuthorizationModel & AuthorizationModel.JwtWebFront)
            == AuthorizationModel.JwtWebFront
        )
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsJwtWebFront,
                x =>
                {
                    //使用应用密钥得到一个加密密钥字节数组
                    var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtWebFrontConfig.Secret);
                    x.RequireHttpsMetadata = true;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    x.Events = new JwtBearerEvents();
                }
            );
        }

        if (
            (authorizeConfig.AuthorizationModel & AuthorizationModel.OAuth2)
            == AuthorizationModel.OAuth2
        )
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsOAuth2,
                options =>
                {
                    options.Authority = authorizeConfig.OAuth2Config.Authority;
                    options.Audience = authorizeConfig.OAuth2Config.Audience;
                    options.RequireHttpsMetadata = authorizeConfig.OAuth2Config.RequireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = authorizeConfig
                            .OAuth2Config
                            .ValidateIssuerSigningKey,
                        ValidateIssuer = authorizeConfig.OAuth2Config.ValidateIssuer,
                        ValidateAudience = authorizeConfig.OAuth2Config.ValidateAudience,
                    };
                }
            );
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        application.UseAuthentication();
        application.UseAuthorization();
        application.UseMiddleware<GirvsTenantClaimsMiddleware>();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    /// <summary>
    /// 根据启用的授权模型选择默认认证 Scheme，必须与已注册的处理器一致。
    /// </summary>
    private static string ResolveDefaultAuthenticationScheme(AuthorizationModel model)
    {
        if ((model & AuthorizationModel.Jwt) == AuthorizationModel.Jwt)
            return GirvsAuthenticationScheme.GirvsJwt;

        if ((model & AuthorizationModel.JwtWebFront) == AuthorizationModel.JwtWebFront)
            return GirvsAuthenticationScheme.GirvsJwtWebFront;

        if ((model & AuthorizationModel.OAuth2) == AuthorizationModel.OAuth2)
            return GirvsAuthenticationScheme.GirvsOAuth2;

        // 未配置任何模型时保持旧配置的宽松行为，交由业务显式指定 Scheme。
        return GirvsAuthenticationScheme.GirvsJwt;
    }

    public int Order { get; } = 99905;
}
