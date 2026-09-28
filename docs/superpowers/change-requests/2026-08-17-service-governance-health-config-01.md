---
change_request_status: rejected
plan_id: 2026-08-17-service-governance-health-config
created_at: 2026-08-17T10:13:00+08:00
---

# Change Request 01：既有工作区改动与基线测试失败

## 触发原因

完整执行启动前发现目标文件存在未提交改动，且受影响 Hosting 测试项目在基线即失败。

## 原条款

1. 正式 Spec FR-7/FR-8：`ServerName` 为空时，Aspire 回退项目元数据类型名、Consul 回退程序集名；`ServerName` 为配置优先的可选入口。
2. 正式 Spec 验收标准 1：`dotnet test Girvs.slnx` 全部通过。

## 证据

1. `Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs` 当前未提交 diff：

   ```diff
   - public string ServerName { get; set; } = "";
   + public string ServerName { get; set; } = ServiceNameResolver.FromAssemblyName();
   ```

2. `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo`：34/35 通过，失败测试为 `ModuleConnectionReferenceTests.Cache连接引用SqlServer资源时组装连接串`。
3. `Girvs.Cache/Configuration/DistributedCacheConfig.cs` 的 `BuildConnectionString` 只支持 `redis` / `redis-synchronized-memory`；`sqlserver` 分支和 `BuildSqlServerConnectionString` 实现均被注释，因此测试抛出 `GirvsException: Resources:cache-db:Type 不支持缓存`。

## 影响

- 保留 `ServerName` 既有改动会将“可选配置优先、空值时调用端回退”改为“默认即程序集归一化名”，并影响 Aspire 资源名与 Consul 注册名的回退语义；这与正式 Spec 不一致。
- 若不修复或不排除 Hosting 基线失败，无法满足全量测试通过的验收标准。
- 修复 SQL Server 缓存连接会修改 `Girvs.Cache`，超出本 Plan 的明确范围外条款。

## 建议选项

1. **按已批准 Spec 执行**：用户保留其他无关改动，但撤销/手工恢复目标文件中 `ServerName` 的预先改动；Hosting 基线失败由用户先修复或提供一个通过的基线后再继续。本选项不需要重新规划。
2. **保留 ServerName 预先改动**：接受其为新需求，重新规划 `ServerName` 默认/回退语义；若仍要求全量测试通过，另行规划 SQL Server 缓存失败修复。需要重新规划。
3. **扩大本次范围**：在重新规划后，同时纳入 ServerName 默认语义变更与 SQL Server 缓存连接支持/测试修复。需要重新规划。
4. **记录既有 Hosting 基线失败并继续**：仅在用户明确放弃“全量测试通过”验收项时可行；这会改变验收标准，需要重新规划。

## 是否必须重新规划

- 选项 1：否。
- 选项 2、3、4：是。

## 处理结论

用户明确要求“先调整不支持 SqlServer”。据此确认 SQL Server 缓存不支持是既有业务语义；只修正与实现矛盾的基线测试，不改 `Girvs.Cache` 行为、不改变本 Plan 验收标准，也不需要重新规划。
