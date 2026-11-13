# 任务 01 验证记录

日期：2026-11-13。实施前基线：`bb7ede63972dbc16b41fef60f57176617305ebc4`，分支：`dev`。

## 实施前基线

| 检查 | 结果 |
| --- | --- |
| SDK | 实际使用 10.0.301；已安装 11.0.100 |
| SDK 配置 | 原值 10.0.0 的特征带无效，dotnet --info 明确报告 Invalid |
| 全仓还原 | 成功 |
| 全仓干净构建 | 成功，154 个警告、0 个错误 |
| 沙箱内测试 | 总计 202，193 通过，9 失败 |
| 沙箱外复跑 | 总计 202，194 通过，8 失败 |

沙箱外仍失败的 8 项均为 RedisFlowStateStoreTests：Docker 命名管道无法连接；本机也找不到 docker 命令。沙箱内另有一项本地 HTTP 测试失败，沙箱外复跑通过。未将这些原有环境问题归因为升级。

复跑各项目结果：

| 项目 | 通过 | 失败 |
| --- | --- | --- |
| Girvs.Core.Tests | 6 | 0 |
| Girvs.Refit.Tests | 9 | 0 |
| Girvs.Claims.Tests | 46 | 0 |
| Girvs.Aspire.Hosting.Tests | 54 | 0 |
| Girvs.ServiceGovernance.Tests | 42 | 0 |
| Girvs.Gateway.Tests | 36 | 8 |
| Sample.Gateway.Tests | 1 | 0 |

原有警告包含依赖审计、可空注解、过时 API 和包元数据告警。完整日志、TRX 保存在本地 `artifacts/`，该目录不提交。

## 实施内容

- 将 SDK 版本修正为已安装的 11.0.100，保留 rollForward 和 allowPrerelease 设置。
- 在共享目标集合追加 net11.0，保留 net8.0、net9.0、net10.0。
- 为 Girvs 和 DynamicWebApi 补充 net11.0 依赖分支。第三方依赖沿用已核对包元数据、构建及运行兼容性的版本，不依据 .NET 主版本号猜测包版本。
- 核心测试增加 net11.0，继续执行原 net10.0 测试。

## 升级后验证

| 检查 | 结果 |
| --- | --- |
| dotnet --version | 11.0.100 |
| 基础引用链还原 | 成功 |
| Girvs + DynamicWebApi 四目标干净构建 | 成功，30 个警告、0 个错误 |
| 核心测试 net10.0 | 6 通过，0 失败 |
| 核心测试 net11.0 | 6 通过，0 失败 |
| 两个 NuGet 包的资产和依赖组 | 均包含 net8.0、net9.0、net10.0、net11.0 |
| 仅使用 PackageReference 的消费者四目标编译 | 成功，0 个警告、0 个错误 |
| 消费者 net10.0、net11.0 运行 | 均选择对应 Girvs 和 DynamicWebApi 资产，HostUseSerilog 构建成功 |

首次 SDK 11 运行写入用户目录的初始化标记受沙箱限制；已通过获准的沙箱外还原完成初始化。net11.0 构建在修改前报告 NETSDK1045，修改后构建及测试通过。

包消费验证使用临时版本 `10.0.0-rc.4.0.3.sdk11-check`，不改仓库发布版本，也不发布远程包。消费者为临时验证夹具；编译四目标，运行本机已安装运行时的 net10.0 和 net11.0。net8.0、net9.0 仅验证构建与资产，未声称运行通过。

## 可复现的基础命令

```powershell
dotnet --version
dotnet restore tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj
dotnet restore Girvs.DynamicWebApi/Girvs.DynamicWebApi.csproj
dotnet build Girvs.DynamicWebApi/Girvs.DynamicWebApi.csproj --no-restore --no-incremental
dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --no-restore
dotnet pack Girvs/Girvs.csproj -c Debug --no-build --no-restore -p:PackageVersion=10.0.0-rc.4.0.3.sdk11-check -o .scratch/dotnet11-upgrade/artifacts/local-feed
dotnet pack Girvs.DynamicWebApi/Girvs.DynamicWebApi.csproj -c Debug --no-build --no-restore -p:PackageVersion=10.0.0-rc.4.0.3.sdk11-check -o .scratch/dotnet11-upgrade/artifacts/local-feed
```

## 阶段限制

本任务仅交付基础引用链。共享目标扩展后，其他库尚待任务 02、03 适配；不能据此宣称整仓 .NET 11 构建已经通过。EF Core 11、实际数据库和主示例编排尚未验收。容器示例已移出升级范围。

## Standards

独立审查未发现需修复的问题：原目标与依赖分支保留，新增分组遵循仓库按目标配置依赖的约定，临时产物已忽略。硬性违规 0 项，需处理的启发式问题 0 项。

## Spec

独立审查发现 1 项 P3 记录问题：准备记录尚未反映已切换 SDK 和核心测试双目标。提交前已同步记录、验证结果和任务状态。代码实现偏差及越界 0 项，未解决发现 0 项。
