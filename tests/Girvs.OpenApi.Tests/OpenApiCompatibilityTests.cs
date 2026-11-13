using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using Girvs;
using Girvs.DynamicWebApi;
using Girvs.FileProvider;
using Girvs.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Girvs.OpenApi.Tests;

[Collection(nameof(OpenApiHostCollection))]
public class OpenApiCompatibilityTests : IAsyncLifetime
{
    private readonly IGirvsFileProvider _originalFileProvider = CommonHelper.DefaultFileProvider;
    private GirvsFileProvider _fileProvider;
    private WebApplication _application;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            if (_application is not null)
            {
                try
                {
                    await _application.StopAsync();
                }
                finally
                {
                    await _application.DisposeAsync();
                }
            }
        }
        finally
        {
            CommonHelper.DefaultFileProvider = _originalFileProvider;
            _fileProvider?.Dispose();
        }
    }

    [Fact]
    public async Task 动态服务接口_保留显式HTTP路由及接口文档()
    {
        var application = CreateApplication(false);
        await application.StartAsync();
        using var client = application.GetTestClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/girvs_openapi/girvs_api.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/compat/dynamic/ping", out _), paths.GetRawText());
        Assert.Equal("dynamic-ok", await client.GetStringAsync("/compat/dynamic/ping"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 文档端点_保留服务地址与Bearer声明(bool enableAuthentication)
    {
        var application = CreateApplication(enableAuthentication);
        await application.StartAsync();
        using var client = application.GetTestClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/girvs_openapi/girvs_api.json"));
        var root = document.RootElement;
        Assert.StartsWith("3.", root.GetProperty("openapi").GetString());
        Assert.Equal("/", root.GetProperty("servers")[0].GetProperty("url").GetString());
        Assert.Equal(2, root.GetProperty("servers").GetArrayLength());
        var operation = root.GetProperty("paths").GetProperty("/probe").GetProperty("get");
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        if (enableAuthentication)
        {
            var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            Assert.Equal("http", scheme.GetProperty("type").GetString());
            Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
        else
        {
            Assert.False(operation.TryGetProperty("security", out _));
        }
    }

    [Theory]
    [InlineData("/girvs_swagger/index.html")]
    [InlineData("/girvs_knife4/index.html")]
    [InlineData("/girvs_scalar")]
    public async Task 开发文档界面_保持现有路由可用(string path)
    {
        var application = CreateApplication(false);
        await application.StartAsync();
        using var client = application.GetTestClient();

        using var initialResponse = await client.GetAsync(path);
        using var redirectedResponse = initialResponse.StatusCode == HttpStatusCode.Found
            ? await client.GetAsync(initialResponse.Headers.Location!)
            : null;
        var response = redirectedResponse ?? initialResponse;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private WebApplication CreateApplication(bool enableAuthentication)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        _fileProvider = new GirvsFileProvider(builder.Environment);
        CommonHelper.DefaultFileProvider = _fileProvider;
        var authentication = builder.Services.AddAuthentication();
        builder.Services.AddControllers().AddApplicationPart(typeof(ProbeDynamicApi).Assembly);
        new DynamicWebApiModule().ConfigureServices(builder.Services, builder.Configuration);
        var module = new OpenApiModule();
        module.ConfigureServices(builder.Services, builder.Configuration);
        if (enableAuthentication)
        {
            authentication.AddScheme<AuthenticationSchemeOptions, ProbeAuthenticationHandler>("Bearer", _ => { });
        }

        var application = _application = builder.Build();
        module.Configure(application, application.Environment);
        application.MapControllers();
        application.MapGet("/probe", () => "ok");
        module.ConfigureMapEndpointRoute(application);
        return application;
    }

    private sealed class ProbeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());
    }
}

[CollectionDefinition(nameof(OpenApiHostCollection), DisableParallelization = true)]
public sealed class OpenApiHostCollection;

[Panda.DynamicWebApi.Attributes.DynamicWebApi]
public class ProbeDynamicApi : IAppWebApiService
{
    [HttpGet("/compat/dynamic/ping")]
    public string Ping() => "dynamic-ok";
}
