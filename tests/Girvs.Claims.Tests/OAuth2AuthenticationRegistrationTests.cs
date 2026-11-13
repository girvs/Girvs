using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Girvs.Claims.Tests;

public class OAuth2AuthenticationRegistrationTests
{
    [Fact]
    public async Task ConfigureServices_启用OAuth2_注册标准JwtBearer配置()
    {
        var services = CreateServices(new AuthorizeConfig
        {
            AuthorizationModel = AuthorizationModel.OAuth2,
            OAuth2Config = new OAuth2Config
            {
                Authority = "https://auth.example.com",
                Audience = "girvs-api",
                RequireHttpsMetadata = true,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = false
            }
        });

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsOAuth2);
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(GirvsAuthenticationScheme.GirvsOAuth2);

        Assert.NotNull(scheme);
        Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
        Assert.Equal("https://auth.example.com", options.Authority);
        Assert.Equal("girvs-api", options.Audience);
        Assert.True(options.RequireHttpsMetadata);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.False(options.TokenValidationParameters.ValidateAudience);
        Assert.Null(options.TokenValidationParameters.SignatureValidator);
    }

    [Fact]
    public async Task ConfigureServices_组合OAuth2与Jwt_同时注册两个Scheme()
    {
        var services = CreateServices(new AuthorizeConfig
        {
            AuthorizationModel = AuthorizationModel.OAuth2 | AuthorizationModel.Jwt
        });

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.NotNull(await schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsOAuth2));
        Assert.NotNull(await schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsJwt));
    }

    private static ServiceCollection CreateServices(AuthorizeConfig authorizeConfig)
    {
        var settings = new AppSettings();
        settings.PreLoadModelConfig();
        settings[nameof(AuthorizeConfig)] = authorizeConfig;
        Singleton<AppSettings>.Instance = settings;

        var services = new ServiceCollection();
        services.AddLogging();
        new GirvsAuthorizeModule().ConfigureServices(
            services,
            new ConfigurationBuilder().Build());
        return services;
    }
}
