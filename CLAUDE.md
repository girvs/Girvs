# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概览

Girvs 是一个基于 .NET 的企业级快速开发框架，采用模块化设计，面向微服务场景。每个功能模块是独立 NuGet 包，通过核心 `Girvs` 模块统一组织。

- **解决方案文件**: `Girvs.slnx`（根目录）
- **多目标框架**: `net8.0;net9.0;net10.0`（在 `Directory.Build.props` 中统一设置）
- **SDK 版本**: `global.json` 中设置为 `10.0.0`（注意：这不是合法的 SDK feature band，本机会提示 `Invalid`，实际可忽略）
- **统一版本号**: 所有模块的 `Version/PackageVersion` 在 `Directory.Build.props` 中集中管理（当前 `10.0.0-rc.1`）
- **构建即打包**: `GeneratePackageOnBuild=true`，每次构建都会在各模块 `bin/<配置>/` 下生成 nupkg（`samples/` 与 `tests/` 通过各自的 `Directory.Build.props` 或 csproj 关闭了打包）

## 常用命令

```bash
# 构建整个解决方案
dotnet build Girvs.slnx

# Release 构建
dotnet build Girvs.slnx -c Release

# 构建单模块
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj

# 清理 + 全量重建
dotnet clean Girvs.slnx
dotnet build Girvs.slnx --no-incremental

# 运行全部测试（tests/ 下三个 xunit 项目，仅 net10.0）
dotnet test Girvs.slnx

# 运行单个测试项目
dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj

# 按名称筛选运行单个测试
dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --filter "FullyQualifiedName~AspireModuleTests"

# 发布到 NuGet（需先 Release 构建；版本号为必填参数，须与 Directory.Build.props 一致）
./nugetpublish.ps1 10.0.0-rc.1        # PowerShell
./push-nuget-packages.sh 10.0.0-rc.1  # Bash 等价脚本

# 运行 Aspire 参照样例（独立解决方案 samples/GirvsAspireSample.slnx，需本机 Docker）
no_proxy=localhost,127.0.0.1,::1 dotnet run --project samples/Sample.AppHost
```

**注意**:
- 测试仅覆盖三个 Aspire 相关模块（`tests/Girvs.Aspire.Tests`、`tests/Girvs.Aspire.Hosting.Tests`、`tests/Girvs.Aspire.Gateway.Tests`），其余模块没有测试项目。
- 根目录 `AGENTS.md` 与本文件内容重叠，其中版本号（9.0.8.1）已过期，以本文件和 `Directory.Build.props` 为准。

## 模块架构

每个模块遵循统一模式：
- 模块入口类 `*Module.cs` 实现 `IAppModuleStartup`，负责该模块的服务注册与中间件配置
- 所有模块依赖核心 `Girvs.csproj`
- 通过 `GirvsHostBuilderManager` 统一配置日志（Serilog）、配置读取和模块启动

| 模块 | 说明 |
|------|------|
| `Girvs` | 核心基础库：DI 扩展、配置管理、日志、`GirvsEngine`（服务定位器）、`GirvsException` |
| `Girvs.EntityFrameworkCore` | EF Core 数据访问：Repository 模式、Unit of Work、多数据库支持、多租户 |
| `Girvs.Cache` | 统一缓存接口 `ICache`，支持 Redis/SQL Server/内存缓存 |
| `Girvs.EventBus` | 基于 DotNetCore.CAP 的分布式事件总线，支持 RabbitMQ/Kafka/Redis Streams |
| `Girvs.Driven` | DDD 支持：MediatR CQRS、FluentValidation、行为管道（缓存、验证） |
| `Girvs.DynamicWebApi` | 自动将应用服务转换为 RESTful API，支持自动路由生成 |
| `Girvs.AuthorizePermission` | 自定义认证方案、基于角色的权限控制、数据规则过滤 |
| `Girvs.Grpc` | gRPC 服务配置、异常拦截器、自动服务注册 |
| `Girvs.Quartz` | Quartz.NET 定时任务调度 |
| `Girvs.Aspire` | .NET Aspire 服务端集成（仅 net10.0）：OpenTelemetry、健康检查、Aspire 服务发现 |
| `Girvs.Aspire.Hosting` | Aspire AppHost 编排扩展（仅 net10.0）：`AsGirvsResource` 显式登记基础资源，`AddGirvsProject` 生成共享配置文件并注入 `GIRVS_SHARED_CONFIG` |
| `Girvs.Aspire.Gateway` | YARP 网关服务发现与约定路由（仅 net10.0）：本地 Aspire 配置发现源 / K8s watch 发现源，服务变化时通过变更令牌热更新路由，详见该模块 `README.md` |
| `Girvs.SignalR` | SignalR 实时通信，JWT 认证集成 |
| `Girvs.Refit` | 声明式 HTTP 客户端封装 |
| `Girvs.AutoMapper` | AutoMapper 自动配置发现与集成 |
| `Girvs.OpenApi` | Swagger/OpenAPI 文档，Bearer Token 认证支持 |
| `Girvs.CodeGenerator` | 代码脚手架工具 |

## 样例与文档

- `samples/`：Girvs + Aspire 端到端参照实现（AppHost + ServiceA/B + Worker），使用独立解决方案 `GirvsAspireSample.slnx`（不进主 `Girvs.slnx`，避免构建即打包），通过 `ProjectReference` 直引框架源码，改框架代码后样例立即生效。各服务暴露 `/selfcheck/*` 自检端点，见 `samples/README.md`。
- `samples/gateway-k8s/`：`Girvs.Aspire.Gateway` 的 kind 集群端到端验证（网关 + 两个 echo 后端 + 完整 K8s 清单），与 AppHost 参照实现是独立话题。
- `docs/aspire/apphost-guide.md`：业务服务接入 Aspire 的迁移指引（框架主线统一使用 Girvs.Aspire）。

## 关键扩展点

### EF Core 数据库提供程序

修改数据库支持时，关注这两个文件：

- `Girvs.EntityFrameworkCore/DbContextExtensions/DataProviderServiceExtensions.cs`
  - `AddGirvsObjectContext()` 通过反射发现所有 `GirvsDbContext` 并注册
- `Girvs.EntityFrameworkCore/DbContextExtensions/DbContextOptionsBuilderExtensions.cs`
  - `ConfigDbContextOptionsBuilder<TDbContext>()` 按 `UseDataType`（MsSql/MySql/…）switch 决定 `Use*`

注意 `#if NET8_0` 等条件编译分支（如 Oracle/InMemory 相关），跨 net8/net9/net10 修改时须检查所有分支。

### 应用服务启动

应用的 Startup 实现 `IGirvsStartup`：

```csharp
public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseGirvsExceptionHandler();
        app.UseRouting();
        app.ConfigureRequestPipeline(env);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.ConfigureEndpointRouteBuilder();
        });
    }
}
```

### 实体定义模式

```csharp
public class Product : AggregateRoot<Guid>,
    IIncludeCreatorId<Guid>,
    IIncludeCreatorName,
    IIncludeMultiTenant<Guid>,
    IIncludeCreateTime,
    ITenantShardingTable,       // 按租户自动分表
    IIncludeMultiTenantName
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

## 多框架依赖管理

每个模块的 `.csproj` 中存在按 `TargetFramework` 分段的条件引用（例如 Serilog 在 net8.0 和 net9.0 版本号不同）。新增 NuGet 依赖时，必须考虑三个目标框架的版本兼容性，并在各分支下分别指定。

## 代码规范

- 命名：类/接口/枚举用 `PascalCase`，接口以 `I` 开头；私有字段用 `_camelCase`；方法/属性用 `PascalCase`
- 全局 using 在各模块的 `GlobalUsings.cs` 中管理，按字母排序
- 业务异常统一使用 `GirvsException`
- 日志使用 `ILogger<T>` 结构化日志
- 异步方法返回 `Task`，禁止 `async void`
- 依赖注入优先使用接口，服务通过构造函数注入
