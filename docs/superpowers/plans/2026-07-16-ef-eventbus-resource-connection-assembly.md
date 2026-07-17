# EF EventBus Resource Connection Assembly Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 EntityFrameworkCore 和 EventBus 的连接串改为启动期从 `Resources` 组装的局部运行时值。

**Architecture:** 配置对象只保留资源引用与模块专属参数。EF 模块在注册 DbContext 前解析主库和读库资源，并注册运行时 `IDataConnectionStringProvider`；EventBus 模块在注册 CAP 前解析持久化数据库和 Redis Transport。Aspire 注入值覆盖相应运行时结果。

**Tech Stack:** .NET 10、xUnit、EntityFramework Core、DotNetCore.CAP。

## Global Constraints

- `Resource.Type` 是字符串；模块自行验证类型。
- EF 主库使用 `ConnectionRef`，读库使用 `ReadConnectionRefs`。
- EventBus Redis 使用 `RedisConfig.DefaultDatabase`，不从资源读取逻辑库编号。

---

### Task 1: EF 资源连接测试与模型

**Files:**
- Modify: `tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs`
- Modify: `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs`
- Modify: `Girvs.EntityFrameworkCore/Configuration/DataBaseConfig.cs`
- Modify: `Girvs.EntityFrameworkCore/GirvsEntityFrameworkCoreModule.cs`
- Create: `Girvs.EntityFrameworkCore/Configuration/IDataConnectionStringProvider.cs`

**Interfaces:**
- Produces: `DataConnectionConfig.BuildConnectionString(Resource)`、`ReadConnectionRefs` 和运行时主从连接串解析。

- [ ] **Step 1: 编写失败测试**

```csharp
var config = new DataConnectionConfig
{
    ConnectionRef = "primary",
    ReadConnectionRefs = ["read-0"],
};
Assert.Equal("mysql", config.BuildConnectionString(resources["primary"]));
```

- [ ] **Step 2: 运行失败测试**

运行：`dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~ModuleConnectionReferenceTests --no-restore --nologo`

预期：因 `ReadConnectionRefs`、`BuildConnectionString` 不存在而失败。

- [ ] **Step 3: 实现运行时解析**

移除配置串属性；主从连接串通过资源生成，Aspire 覆盖写入运行时解析结果。

- [ ] **Step 4: 运行测试确认通过**

运行：`dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --no-restore --nologo`

### Task 2: EventBus 资源连接测试与模型

**Files:**
- Modify: `tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs`
- Modify: `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs`
- Modify: `Girvs.EventBus/Configuration/EventBusConfig.cs`
- Modify: `Girvs.EventBus/EventBusModule.cs`

**Interfaces:**
- Produces: `EventBusConfig.BuildPersistenceConnectionString(Resource)` 和 `RedisConfig.BuildConnectionString(Resource)`。

- [ ] **Step 1: 编写失败测试**

```csharp
var redis = new RedisConfig { ConnectionRef = "redis", DefaultDatabase = 2 };
Assert.Equal("redis:6379,defaultDatabase=2", redis.BuildConnectionString(resources["redis"]));
```

- [ ] **Step 2: 运行失败测试**

运行：`dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~ModuleConnectionReferenceTests --no-restore --nologo`

预期：因 `DefaultDatabase`、构造方法不存在而失败。

- [ ] **Step 3: 实现模块局部连接串**

移除 `DbConnectionString`、`RedisConnectionString`，在 `EventBusModule` 调用 CAP 前解析资源；Aspire 注入值优先。

- [ ] **Step 4: 运行完整相关测试**

运行：`dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo && dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --no-restore --nologo`
