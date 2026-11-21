# .NET 11 库支持

当前分支为全部 17 个库增加 `net11.0` 支持，保留各库已有目标。构建使用 `global.json` 指定的 .NET SDK 11.0.100。`Directory.Build.props` 维护的基线包版本仍为 10.0.0-rc.4.0.3；正式包版本通过标签流水线发布，见下文「发布流程」。

## 模块目标

| 模块 | 支持的目标框架 |
| --- | --- |
| Girvs | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Aspire.Hosting | net8.0、net9.0、net10.0、net11.0 |
| Girvs.AuthorizePermission | net8.0、net9.0、net10.0、net11.0 |
| Girvs.AutoMapper | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Cache | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Consul | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Driven | net8.0、net9.0、net10.0、net11.0 |
| Girvs.DynamicWebApi | net8.0、net9.0、net10.0、net11.0 |
| Girvs.EntityFrameworkCore | net8.0、net9.0、net10.0、net11.0 |
| Girvs.EventBus | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Gateway | net10.0、net11.0 |
| Girvs.Grpc | net8.0、net9.0、net10.0、net11.0 |
| Girvs.OpenApi | net9.0、net10.0、net11.0 |
| Girvs.Quartz | net8.0、net9.0、net10.0、net11.0 |
| Girvs.Refit | net8.0、net9.0、net10.0、net11.0 |
| Girvs.ServiceGovernance | net10.0、net11.0 |
| Girvs.SignalR | net8.0、net9.0、net10.0、net11.0 |

`Girvs.Aspire`、`Girvs.Aspire.Gateway`、`Girvs.CodeGenerator` 目录当前没有 `.csproj`，不作为独立库构建或打包。实际发布模块以解决方案中的库项目为准。

## 新增目标的依赖选择

- EF Core 各组件统一使用 11.0.0；MySQL 提供程序为 Microting.EntityFrameworkCore.MySql 11.0.0 正式版，并显式选择 MySqlConnector 3.0.0。
- ASP.NET Core 的 JwtBearer、SignalR、缓存和 OpenAPI 组件使用 11.0.0。JwtBearer 与 SQL Server 缓存的传递依赖需要新版 IdentityModel，新增目标对齐为 8.23.0。
- OpenAPI 使用 Swashbuckle.AspNetCore 11.0.1 与 Microsoft.OpenApi 3 系列；旧目标继续使用各自的现有版本。
- net11.0 目标后续升级的其他依赖：Quartz 3.22.10、MediatR 15.0.0、FluentValidation 12.2.0、DotNetCore.CAP 10.0.2、BouncyCastle.Cryptography 2.7.0、System.Data.SqlClient 4.9.1、SQLitePCLRaw.lib.e_sqlite3 3.53.3；net8–net10 目标保持原有版本。
- 其余第三方包保留已验证兼容的版本，不按 .NET 主版本号机械升级。AutoMapper、Aspire.Hosting、YARP、CAP、Refit、gRPC 等通过构建、包资产及相关测试验证。
- Refit 的统一服务目录、配置与注册分支，以及 OpenAPI 的 XML 注释和 Scalar UI 分支，已由仅匹配 NET10_0 改为覆盖 NET10_0_OR_GREATER。

## 构建与测试

```powershell
dotnet --version
dotnet restore Girvs.slnx
dotnet build Girvs.slnx --no-restore --no-incremental
dotnet build Girvs.slnx -c Release --no-restore
dotnet test Girvs.slnx --no-restore
dotnet test tests/Girvs.OpenApi.Tests/Girvs.OpenApi.Tests.csproj -f net11.0
```

所有库测试项目均覆盖 .NET 10 和 .NET 11。运行测试需安装这两个运行时；构建 .NET 8、9 资产不代表已在相应运行时执行测试。

当前主示例与 AppHost 仍单独按示例任务跟踪，库支持进度不受实际数据库验收阻挡。容器示例不在本次适配范围内。

## 发布流程

推送 `v*` 或数字开头的 Git 标签触发 `.github/workflows/publish-nuget.yml`：流水线以标签号（去掉 `v` 前缀）覆盖 `Version`/`PackageVersion` 构建 Release 包，先运行全部测试，再打包并推送到 nuget.org。

`v11.0.0-preview1.0` 至 `v11.0.0-preview1.9`（2026-11-16 至 2026-11-20）已按该流程发布成功。日常开发不要修改 `Directory.Build.props` 中的基线版本号，发布版本一律由标签注入。

## 验证边界

新增 OpenAPI 回归通过公共 HTTP 端点检查文档、Bearer 声明、服务地址及 Swagger、Knife4j、Scalar 的现有 UI 路由。Hosting、服务治理、Refit、身份和网关等沿用现有行为测试，分别在 .NET 10、11 执行。

需要 Docker 的 Redis 集成测试在无 Docker 的机器上仍会失败，应与纯代码回归区分记录，不将过滤后的测试结果称为完整测试通过。实际 MySQL、SQL Server 的读写、迁移和延迟加载仍需临时数据库环境验收；模型加载、SQL、建表脚本及快照检查不能替代实际数据库验收。
