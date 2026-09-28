# Girvs.ServiceGovernance 实施计划

> **执行要求：** 使用 `executing-plans`；功能与修复遵循 TDD；完成前执行代码审查和全量验证。

## 目标

将 `Girvs.Aspire` 重命名为 `Girvs.ServiceGovernance`，合并 Consul WebApi/gRPC 注册能力，并统一 net10.0 下 Girvs.Refit 的发现 Provider。

## 实施约束

- `Girvs.ServiceGovernance` 显式仅支持 net10.0。
- `Girvs.Consul` 不修改。
- `Girvs.Refit` 保持 net8.0/net9.0/net10.0。
- 配置节点为 `ModuleConfigurations:ServiceGovernanceConfig`。
- gRPC Health 始终映射。
- 不自动提交 Git commit。

## Task 1：隔离与基线

- [x] 创建 `feature/service-governance` worktree。
- [x] 运行 `dotnet test Girvs.slnx --nologo`。
- [x] 记录既有失败：Gateway 映射 2 项、Docker/Testcontainers 8 项、Aspire.Hosting 1 项。
- [x] 确认原 `Girvs.Aspire.Tests` 与 `Girvs.Refit.Tests` 全绿。

## Task 2：纯重命名

- [x] `Girvs.Aspire` → `Girvs.ServiceGovernance`。
- [x] `AspireModule` → `ServiceGovernanceModule`。
- [x] `GirvsAspireSerilogHook` → `GirvsSerilogOtlpHook`。
- [x] 更新 Girvs 核心反射契约。
- [x] 更新测试项目、解决方案和样例引用。
- [x] 运行重命名后的测试，保持行为不变。

## Task 3：配置与互斥 Provider（TDD）

- [x] RED：配置类型不存在。
- [x] GREEN：实现 `ServiceGovernanceConfig`、`ServiceDiscoveryProvider`、`ConsulServerModel`。
- [x] RED：Consul 模式仍注册 Aspire Service Discovery。
- [x] GREEN：按 Provider 互斥注册。
- [x] 验证真实 `IServiceEndpointProviderFactory` 服务描述符。

## Task 4：Consul 注册（TDD）

- [x] RED：缺少内部 Consul 注册器和注册信息构造器。
- [x] GREEN：实现 `IConsulServiceRegistrar` / `ConsulServiceRegistrar`。
- [x] 测试 WebApi 和 gRPC `AgentServiceRegistration`。
- [x] 测试 WebApi 不注册 `IConsulClient`，gRPC 注册单例。
- [x] 测试注册器异常时 Module 软失败。

## Task 5：gRPC Health（TDD）

- [x] RED：缺少 `HealthCheckService`。
- [x] GREEN：实现 `Check` / `Watch`，返回 `Serving`。
- [x] 验证实现 `IAppGrpcService`。

## Task 6：Girvs.Refit（TDD）

- [x] RED：net10.0 无 ServiceGovernance 引用。
- [x] GREEN：添加条件项目引用和条件编译。
- [x] 测试 Aspire/Consul Resolver 选择。
- [x] 测试 ConsulAddress 优先级与回退。
- [x] 构建 net8.0/net9.0/net10.0。

## Task 7：构建和测试

- [x] `dotnet build Girvs.slnx -c Release --nologo`。
- [x] `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj`（27/27）。
- [x] `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj`（10/10）。
- [x] 复跑全量测试并与基线比较；除并行执行引起的一次测试文件竞态外，既有失败集合未增加，单独复跑 ServiceGovernance 连续两次 27/27。

## Task 8：完成流程

- [x] 请求代码审查。
- [x] 处理有效审查意见；复核结论为无 Critical/Important 阻塞问题。
- [x] 使用 `verification-before-completion` 运行最终验证。
- [x] 使用 `finishing-a-development-branch` 提供集成选项；用户已选择本地合并，代码提交 `bbff4ec` 已 fast-forward 合并到 `girvs_aspire`，本次文档提交尚未合并。
