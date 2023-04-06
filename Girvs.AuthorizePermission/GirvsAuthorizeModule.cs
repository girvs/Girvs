namespace Girvs.AuthorizePermission;

public class GirvsAuthorizeModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

        var authenticationBuilder =
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme);

        if ((authorizeConfig.AuthorizationModel & AuthorizationModel.Jwt) == AuthorizationModel.Jwt)
        {
            authenticationBuilder
                .AddJwtBearer(GirvsAuthenticationScheme.GirvsJwt, x =>
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
                });
        }

        if ((authorizeConfig.AuthorizationModel & AuthorizationModel.IdentityServer4) ==
            AuthorizationModel.IdentityServer4)
        {
            authenticationBuilder
                .AddJwtBearer(GirvsAuthenticationScheme.GirvsIdentityServer4, options =>
                {
                    options.Authority = authorizeConfig.IdentityServer4Config.ServerHost;
                    options.RequireHttpsMetadata = authorizeConfig.IdentityServer4Config.UseHttps;
                    options.SaveToken = false;
                    options.Audience = authorizeConfig.IdentityServer4Config.ApiResourceName;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            // services.AddAuthorization(options =>
            // {
            //     options.AddPolicy(config.Service.PolicyName, policy =>
            //     {
            //         policy.RequireAuthenticatedUser();
            //         policy.RequireClaim("scope", config.Service.ServiceName);
            //     });
            // });

            // services.AddAuthentication("token")
            //
            //     // JWT tokens
            //     .AddJwtBearer("token", options =>
            //     {
            //         options.Authority = authorizeConfig.IdentityServer4Config.ServerHost;
            //         options.Audience = authorizeConfig.IdentityServer4Config.ApiResourceName;
            //         options.RequireHttpsMetadata = authorizeConfig.IdentityServer4Config.UseHttps;
            //         //options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
            //
            //         // if token does not contain a dot, it is a reference token
            //         //options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
            //     })
            //
            //     // reference tokens
            //     .AddOAuth2Introspection("introspection", options =>
            //     {
            //         options.Authority = authorizeConfig.IdentityServer4Config.ServerHost;
            //         options.ClientId = authorizeConfig.IdentityServer4Config.ApiResourceName;
            //         options.ClientSecret = authorizeConfig.IdentityServer4Config.ApiSecret;
            //     });
        }
    }

    public void Configure(IApplicationBuilder application)
    {
        application.UseAuthentication();
        application.UseAuthorization();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
    }

    public int Order { get; } = 99905;
}