# Cache Resource Connection Assembly Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Girvs.Cache 仅保存模块专属缓存设置，并从 `Resources` 组装 SqlServer、Redis 与 RedisSynchronizedMemory 的实际连接串。

**Architecture:** `DistributedCacheConfig` 删除持久化连接串，新增 `DefaultDatabase`。`GirvsCacheModule` 在注册具体分布式缓存前按 `ConnectionRef` 获取资源，并仅在对应缓存类型下构造局部连接串；Aspire 的 `girvs-cache` 注入值最后覆盖该局部结果。

**Tech Stack:** .NET 10、xUnit、Microsoft.Extensions.Caching.StackExchangeRedis、Microsoft.Extensions.Caching.SqlServer。

## Global Constraints

- `Resource.Type` 保持字符串，Girvs 核心不新增资源适配器。
- `Resource.Settings` 的值为字符串；Redis 多 endpoint 使用逗号分隔。
- 未配置 `ConnectionRef`、资源缺失或类型不匹配时抛出 `GirvsException`。

---

### Task 1: Cache 配置与资源组装测试

**Files:**
- Modify: `tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs`
- Modify: `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs`

**Interfaces:**
- Consumes: `DistributedCacheConfig.ConnectionRef`、`DefaultDatabase`。
- Produces: 覆盖 Redis 与 SqlServer 资源连接串组装、Aspire 覆盖优先级的失败测试。

- [ ] **Step 1: 编写失败测试**

```csharp
var cache = new DistributedCacheConfig
{
    ConnectionRef = "platform-redis",
    DefaultDatabase = 3,
};

var connectionString = cache.CreateConnectionString(new Resource
{
    Type = "redis",
    Settings = new Dictionary<string, string> { ["Endpoints"] = "redis:6379" },
});

Assert.Equal("redis:6379,defaultDatabase=3", connectionString);
```

- [ ] **Step 2: 运行测试确认失败**

运行：`dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~ModuleConnectionReferenceTests --no-restore --nologo`

预期：因 `DefaultDatabase` 与 `CreateConnectionString` 尚不存在而编译失败。

### Task 2: 在 Cache 模块中组装连接串

**Files:**
- Modify: `Girvs.Cache/Configuration/DistributedCacheConfig.cs`
- Modify: `Girvs.Cache/Configuration/CacheConfig.cs`
- Modify: `Girvs.Cache/GirvsCacheModule.cs`
- Modify: `Girvs.Cache/CacheImps/MsSqlServerCacheManager.cs`

**Interfaces:**
- Consumes: `Resource`、`DistributedCacheType`、`ConnectionRef`。
- Produces: `CreateConnectionString(Resource resource)` 与仅在 SqlServer、Redis、RedisSynchronizedMemory 注册分支使用的局部连接串。

- [ ] **Step 1: 实现最小配置变更**

```csharp
public int DefaultDatabase { get; set; }

public string CreateConnectionString(Resource resource)
{
    // SqlServer 校验 sqlserver 类型；Redis 校验 redis 类型，追加 DefaultDatabase。
}
```

- [ ] **Step 2: 移除 ConnectionString 状态依赖**

在 Cache 模块的 `SqlServer`、`Redis`、`RedisSynchronizedMemory` 分支中，以局部 `connectionString` 传入缓存选项；`MsSqlServerCacheManager` 构造时同样根据资源生成连接串。

- [ ] **Step 3: 运行目标测试确认通过**

运行：`dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo`

预期：全部通过。

### Task 3: 验证 Aspire 覆盖与回归

**Files:**
- Modify: `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs`

**Interfaces:**
- Consumes: `CacheConfig.ApplyAspireConnectionString(IConfiguration)`。
- Produces: Aspire 注入连接串覆盖资源组装结果的测试。

- [ ] **Step 1: 调整测试以断言覆盖结果**

```csharp
var connectionString = cacheConfig.GetConnectionString(resource, configuration);
Assert.Equal("aspire-redis:6379", connectionString);
```

- [ ] **Step 2: 运行 Cache 与 Aspire 测试**

运行：`dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --no-restore --nologo`

预期：全部通过。
