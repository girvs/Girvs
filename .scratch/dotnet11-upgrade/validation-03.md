# 全部库 .NET 11 支持验证记录

日期：2026-11-13。本轮基线：`abf8703`，分支：`dev`。

用户明确要求全部库增加 .NET 11 支持。实际数据库验收与库适配分别跟踪，不再以数据库环境缺失阻挡其余库推进。

## 交付范围

全部 17 个实际库项目支持 net11.0，并保留已有目标：

| 目标集合 | 模块 |
| --- | --- |
| net8.0、net9.0、net10.0、net11.0 | Girvs、Girvs.Aspire.Hosting、Girvs.AuthorizePermission、Girvs.AutoMapper、Girvs.Cache、Girvs.Consul、Girvs.Driven、Girvs.DynamicWebApi、Girvs.EntityFrameworkCore、Girvs.EventBus、Girvs.Grpc、Girvs.Quartz、Girvs.Refit、Girvs.SignalR |
| net9.0、net10.0、net11.0 | Girvs.OpenApi |
| net10.0、net11.0 | Girvs.Gateway、Girvs.ServiceGovernance |

Girvs.Aspire、Girvs.Aspire.Gateway、Girvs.CodeGenerator 只有遗留产物目录，没有独立项目文件，不计为待迁移库。

## 实现要点

- 为各库的按目标依赖组追加 net11.0，显式覆盖目标集合的项目增加新目标；继承共享目标的 AutoMapper、Aspire.Hosting 等经实际构建和包资产核查确认覆盖新目标。
- JwtBearer、SignalR、缓存、OpenAPI 的 Microsoft 组件在新目标使用 11.0.0；IdentityModel 对齐为 8.19.2，解决 JwtBearer 11 与 SqlClient 7 的依赖降级冲突。
- OpenAPI 新目标使用 Swashbuckle 11.0.1，兼容 Microsoft.OpenApi 3 系列；旧目标保留原有版本。
- Refit 的统一目录解析和注册、OpenAPI 的 XML 注释和 Scalar 分支改用 NET10_0_OR_GREATER，防止新目标落回旧 API 分支。
- 所有库测试项目新增 net11.0 执行覆盖，保留 net10.0。
- 新增公共 HTTP 边界测试，覆盖 OpenAPI 文档、Bearer 声明、服务地址、三种 UI 路由以及显式标注的动态 API 路由。

## 构建、打包及包消费

| 检查 | 结果 |
| --- | --- |
| 整仓依赖还原 | 成功 |
| 整仓干净构建 | 成功，217 个警告、0 个错误 |
| 17 个库打包 | 成功 |
| 包内程序集和依赖组 | 全部存在 net11.0，并保留原目标资产 |
| 17 包组成的独立 PackageReference 消费者 | net10.0、net11.0 均编译通过，0 警告、0 错误 |
| 消费者运行 | 两个框架均选中对应目标的 17 个库，并成功加载全部公开类型 |

包消费验证使用临时版本 `10.0.0-rc.4.0.3.net11-all-check` 与本地源，不修改仓库发布版本、不发布远程包。临时消费者、包、详细日志和 TRX 位于已忽略的 artifacts 目录。

## 最终测试结果

| 项目 | net10.0 | net11.0 |
| --- | --- | --- |
| Girvs.Core.Tests | 6 通过 | 6 通过 |
| Girvs.Claims.Tests | 46 通过 | 46 通过 |
| Girvs.Aspire.Hosting.Tests | 54 通过 | 54 通过 |
| Girvs.Refit.Tests | 9 通过 | 9 通过 |
| Girvs.ServiceGovernance.Tests | 42 通过 | 42 通过 |
| Girvs.EntityFrameworkCore.Tests | 4 通过 | 6 通过 |
| Girvs.OpenApi.Tests | 6 通过 | 6 通过 |
| Girvs.Gateway.Tests | 36 通过、8 失败 | 36 通过、8 失败 |
| Sample.Gateway.Tests | 1 通过 | 尚未纳入示例任务 |

总计 425 项，409 通过，16 失败。已逐项解析 TRX，所有失败均来自 RedisFlowStateStoreTests，其 Docker 命名管道不可用，与升级前的 8 项环境失败一致，仅因双框架执行计为 16 项。全套测试退出码为 1，未将其表述为全部通过，也未通过删除或过滤用例规避失败。

OpenAPI 夹具创建完整 Girvs 文件提供程序并明确标注动态 API；资源由 xUnit 异步生命周期负责停止、释放及恢复原静态值，禁并行集合隔离全局状态。文档路径和 HTTP 返回断言均保留。

## 可复现命令

```powershell
dotnet restore Girvs.slnx
dotnet build Girvs.slnx --no-restore --no-incremental
dotnet test Girvs.slnx --no-restore
dotnet test tests/Girvs.OpenApi.Tests/Girvs.OpenApi.Tests.csproj --no-restore
dotnet pack Girvs.slnx -c Debug --no-build --no-restore -p:PackageVersion=10.0.0-rc.4.0.3.net11-all-check -o .scratch/dotnet11-upgrade/artifacts/all-local-feed
```

## Standards

规范审查发现 1 项测试夹具生命周期问题：ApplicationStopped 回调未覆盖仅 DisposeAsync 的路径，静态文件提供程序也未恢复。已改用 IAsyncLifetime 显式清理并隔离全局状态；修复后相关测试在两个框架各 6 项通过，审查复核确认问题已解决。其余库目标、依赖隔离与条件编译符合仓库约定，未解决发现 0 项。

## Spec

规格审查未发现本轮新增偏差。17 个库均支持新目标并保留旧目标。动态 API 用例经过真实模块注册、MVC 发现、HTTP 请求和 OpenAPI 文档入口；其覆盖范围是显式标注场景，未宣称无标注服务也已验证。

## 仍待整体升级验收的内容

实际 MySQL、SQL Server 读写、迁移、仓储和延迟加载验收仍属于任务 02；主示例服务、AppHost 与示例网关测试的新增目标属于任务 04。它们不再阻挡库支持交付，也未被标记为已经完成。容器示例保持排除在适配范围之外。
