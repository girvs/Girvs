# OAuth 2.0 认证服务器配置迁移实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `Girvs.AuthorizePermission` 的 IdentityServer4 专用配置破坏性迁移为通用 OAuth 2.0 认证服务器 JWT Bearer 配置。

**Architecture:** 保持现有 `IAppModuleStartup` 和 `AddJwtBearer` 注册结构，只替换公开配置模型、Flags 枚举和认证 Scheme，并使用 ASP.NET Core 标准 JWT Bearer 选项完成认证服务器元数据及令牌校验。注册行为通过 `IOptionsMonitor<JwtBearerOptions>` 做容器级测试，确保配置映射和多模式组合注册正确。

**Tech Stack:** .NET 10、ASP.NET Core Authentication/JwtBearer、Microsoft.IdentityModel.Tokens、xUnit

## Global Constraints

- 直接删除全部 `IdentityServer4` 专用命名，不提供兼容别名。
- `AuthorizationModel.OAuth2` 必须保留原位值 `2`。
- `OAuth2Config` 使用 `Authority`、`Audience`、`RequireHttpsMetadata` 标准术语。
- 保留三个 Token 校验开关及原默认值，不改变 `Jwt` 和 `JwtWebFront` 行为。
- 删除未使用的 `ApiSecret` 和自定义 `SignatureValidator`。
- 不修改工作区中与本任务无关的已有改动。

---

### Task 1: 用测试定义 OAuth 2.0 注册契约

**Files:**
- Create: `tests/Girvs.Claims.Tests/OAuth2AuthenticationRegistrationTests.cs`

**Interfaces:**
- Consumes: `GirvsAuthorizeModule.ConfigureServices(IServiceCollection, IConfiguration)`、`Singleton<AppSettings>.Instance`
- Produces: OAuth 2.0 配置映射和 Flags 组合注册的回归测试

- [x] **Step 1: 编写失败的配置映射测试**

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Girvs.Claims.Tests;

public class OAuth2AuthenticationRegistrationTests
{
    [Fact]
    public void ConfigureServices_启用OAuth2_注册标准JwtBearer配置()
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
        var scheme = schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsOAuth2)
            .GetAwaiter().GetResult();
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
```

- [x] **Step 2: 运行测试并确认因新 API 尚不存在而失败**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~OAuth2AuthenticationRegistrationTests --nologo`

Expected: FAIL，编译错误包含 `AuthorizationModel` 不存在 `OAuth2`，或找不到 `OAuth2Config` / `GirvsOAuth2`。

- [x] **Step 3: 增加 Flags 组合注册测试**

在同一测试类增加：

```csharp
[Fact]
public void ConfigureServices_组合OAuth2与Jwt_同时注册两个Scheme()
{
    var services = CreateServices(new AuthorizeConfig
    {
        AuthorizationModel = AuthorizationModel.OAuth2 | AuthorizationModel.Jwt
    });

    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

    Assert.NotNull(schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsOAuth2)
        .GetAwaiter().GetResult());
    Assert.NotNull(schemes.GetSchemeAsync(GirvsAuthenticationScheme.GirvsJwt)
        .GetAwaiter().GetResult());
}
```

- [x] **Step 4: 再次运行测试并确认仍以相同缺失 API 原因失败**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~OAuth2AuthenticationRegistrationTests --nologo`

Expected: FAIL，失败仍指向尚未实现的 OAuth 2.0 API，而不是测试装配错误。

### Task 2: 实现破坏性的 OAuth 2.0 命名与标准注册

**Files:**
- Modify: `Girvs.AuthorizePermission/Configuration/AuthorizeConfig.cs`
- Modify: `Girvs.AuthorizePermission/GirvsAuthenticationScheme.cs`
- Modify: `Girvs.AuthorizePermission/GirvsAuthorizeModule.cs`
- Test: `tests/Girvs.Claims.Tests/OAuth2AuthenticationRegistrationTests.cs`

**Interfaces:**
- Consumes: Task 1 定义的 `OAuth2Config`、`AuthorizationModel.OAuth2`、`GirvsOAuth2` 契约
- Produces: `OAuth2Config` 配置类型、OAuth 2.0 Flags 值和标准 JWT Bearer Scheme

- [x] **Step 1: 替换公开配置模型与 Flags 名称**

将 `AuthorizeConfig` 的外部认证配置改为：

```csharp
public OAuth2Config OAuth2Config { get; set; } = new OAuth2Config();
```

以如下类型完整替换 `IdentityServer4Config`：

```csharp
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
```

枚举保持位值不变：

```csharp
[Flags]
public enum AuthorizationModel : long
{
    Jwt = 1,
    OAuth2 = 2,
    JwtWebFront = 4
}
```

- [x] **Step 2: 替换认证 Scheme 常量**

将专用常量替换为：

```csharp
public const string GirvsOAuth2 = "GirvsOAuth2";
```

- [x] **Step 3: 改为标准 OAuth 2.0 JWT Bearer 注册**

用以下分支完整替换 IdentityServer4 分支：

```csharp
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
                ValidateAudience = authorizeConfig.OAuth2Config.ValidateAudience
            };
        }
    );
}
```

同时删除不再使用的 `using Microsoft.IdentityModel.JsonWebTokens;`。

- [x] **Step 4: 运行定向测试并确认通过**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~OAuth2AuthenticationRegistrationTests --nologo`

Expected: PASS，2 个测试通过。

- [x] **Step 5: 提交行为变更**

```bash
git add Girvs.AuthorizePermission tests/Girvs.Claims.Tests/OAuth2AuthenticationRegistrationTests.cs
git commit -m "refactor: 认证服务器改用 OAuth2 标准配置"
```

### Task 3: 验证模块兼容性与专用命名清理

**Files:**
- Verify: `Girvs.AuthorizePermission/**`
- Verify: `tests/Girvs.Claims.Tests/**`
- Verify: repository source and configuration files

**Interfaces:**
- Consumes: Task 2 完成的公开 API
- Produces: 多目标框架构建、相关测试和全仓命名清理证据

- [x] **Step 1: 扫描生产代码、测试和示例配置中的旧命名**

Run: `rg -n "IdentityServer4|GirvsIdentityServer4" . --glob '!docs/superpowers/**' --glob '!**/bin/**' --glob '!**/obj/**'`

Expected: 无输出，退出码为 `1`。

- [x] **Step 2: 构建 AuthorizePermission 的全部目标框架**

Run: `dotnet build Girvs.AuthorizePermission/Girvs.AuthorizePermission.csproj --nologo`

Expected: PASS，`net8.0`、`net9.0`、`net10.0` 均为 0 个错误。

- [x] **Step 3: 运行 Claims 测试项目**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo`

Expected: PASS，全部测试通过。

- [x] **Step 4: 检查补丁格式和任务文件范围**

Run: `git diff --check`

Expected: 无新增空白错误；工作区其他既有文件的换行警告不属于本任务。

Run: `git status --short`

Expected: 本任务实现文件已由 Task 2 提交；只保留用户原有的无关工作区修改以及本实施计划文档。

- [x] **Step 5: 提交实施计划文档**

```bash
git add docs/superpowers/plans/2026-07-22-oauth2-authorization-server.md
git commit -m "docs: 规划 OAuth2 认证服务器配置迁移"
```
