# 共享配置文件分发机制实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 服务端新增 `GIRVS_SHARED_CONFIG` 低优先级配置源;AppHost 端以 `AsGirvsResource` 显式登记容器资源并生成共享配置文件分发;回滚删除全部 `ConnectionStrings__girvs-*` 注入与 `[DependsOn]` 自动推断机制。

**Architecture:** 服务获取连接信息只有一条路——`AppSettings.Resources[ConnectionRef]` 组装连接串;共享文件作为最低优先级配置源进入服务配置系统,服务本地配置天然覆盖。AppHost 编排什么容器由开发者显式声明,容器地址在启动期写入运行时生成的共享文件。

**Tech Stack:** .NET 8/9/10(Girvs 核心多目标)、Aspire.Hosting 13.4.6(仅 net10.0)、xunit。

**Spec:** `docs/superpowers/specs/2026-07-17-shared-config-file-distribution-design.md`

## Global Constraints

- 环境变量名固定 `GIRVS_SHARED_CONFIG`;publish 模式约定路径固定 `/girvs-config/girvs.shared.json`;手写共享文件名固定 `girvs.shared.json`(AppHost 项目目录);运行时生成文件固定 `obj/girvs.shared.runtime.json`。
- Girvs 各功能模块(Cache / EF / EventBus)的 `BuildConnectionString` 与 Resources 消费逻辑**不得修改**,本计划只删除其中的 Aspire 覆盖分支。
- 所有新增代码注释使用中文;commit message 使用中文 Conventional Commits。
- 主解决方案构建命令:`dotnet build Girvs.slnx`;测试命令:`dotnet test Girvs.slnx`(或按项目)。每个任务结束时两者必须通过。
- `Directory.Build.props` 版本号、多目标框架配置不动。

---

### Task 1: 服务端 GIRVS_SHARED_CONFIG 低优先级配置源

**Files:**
- Modify: `Girvs/GirvsHostBuilderManager.cs:70-73`(`HostUseGirvsConfig` 开头)
- Test: `tests/Girvs.Aspire.Tests/SharedConfigSourceTests.cs`(新建)

**Interfaces:**
- Consumes: 现有 `GirvsHostBuilderManager.HostUseGirvsConfig(IConfigurationBuilder, IWebHostEnvironment, string[], string[])`
- Produces: 环境变量 `GIRVS_SHARED_CONFIG` 指向的 JSON 文件作为最低优先级配置源被加载(后续任务的 AppHost 端依赖此契约)

- [ ] **Step 1: 写失败测试**

新建 `tests/Girvs.Aspire.Tests/SharedConfigSourceTests.cs`:

```csharp
using Girvs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Girvs.Aspire.Tests;

/// <summary>
/// 验证 GIRVS_SHARED_CONFIG 共享配置源:作为最低优先级加载,服务本地配置覆盖共享值。
/// 测试通过进程环境变量与 AppContext.BaseDirectory 下的 appsettings.json 驱动,
/// 全部用例放在同一个 class 内以保证串行执行,避免进程级状态互相污染。
/// </summary>
public class SharedConfigSourceTests : IDisposable
{
    private const string EnvName = "GIRVS_SHARED_CONFIG";
    private readonly string _sharedFile = Path.Combine(
        Path.GetTempPath(), $"girvs-shared-{Guid.NewGuid():N}.json");
    private readonly string _localAppSettings = Path.Combine(
        AppContext.BaseDirectory, "appsettings.json");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvName, null);
        if (File.Exists(_sharedFile)) File.Delete(_sharedFile);
        if (File.Exists(_localAppSettings)) File.Delete(_localAppSettings);
    }

    private static IConfiguration BuildConfig()
    {
        var builder = new ConfigurationBuilder();
        builder.HostUseGirvsConfig(new TestWebHostEnvironment());
        return builder.Build();
    }

    [Fact]
    public void 共享文件被加载()
    {
        File.WriteAllText(_sharedFile,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"shared:6379"}}}}""");
        Environment.SetEnvironmentVariable(EnvName, _sharedFile);

        var config = BuildConfig();

        Assert.Equal("shared:6379", config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 服务本地appsettings覆盖共享值()
    {
        File.WriteAllText(_sharedFile,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"shared:6379"}}}}""");
        File.WriteAllText(_localAppSettings,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"local:6379"}}}}""");
        Environment.SetEnvironmentVariable(EnvName, _sharedFile);

        var config = BuildConfig();

        Assert.Equal("local:6379", config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 未设置环境变量时不加载共享文件()
    {
        Environment.SetEnvironmentVariable(EnvName, null);

        var config = BuildConfig();

        Assert.Null(config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 共享文件不存在时静默降级()
    {
        Environment.SetEnvironmentVariable(EnvName,
            Path.Combine(Path.GetTempPath(), "girvs-not-exists.json"));

        var config = BuildConfig(); // 不抛异常即通过

        Assert.Null(config["Resources:platform-redis:Settings:Endpoints"]);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "";
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
```

注意:`HostUseGirvsConfig` 中 `AddJsonFile("appsettings.json")` 的相对路径基于 `AppContext.BaseDirectory`(测试输出目录),所以本地文件写到该目录。若测试项目缺少 `Microsoft.Extensions.FileProviders.Abstractions` 的 `NullFileProvider`,它由 `FrameworkReference Microsoft.AspNetCore.App` 提供,无需加包。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --filter "FullyQualifiedName~SharedConfigSourceTests"`
Expected: `共享文件被加载` FAIL(断言 null != "shared:6379");其余两个否定性用例此时可能已通过,属正常。

- [ ] **Step 3: 实现**

修改 `Girvs/GirvsHostBuilderManager.cs` 的 `HostUseGirvsConfig`,在 `config.Sources.Clear();` 之后、`config.AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, ...)` 之前插入:

```csharp
        //清除原有的源
        config.Sources.Clear();

        // 共享配置文件(GIRVS_SHARED_CONFIG 指向,由 Aspire AppHost 注入或 K8s ConfigMap 挂载):
        // 作为最低优先级配置源,服务本地 appsettings*.json 与环境变量天然覆盖其中的同名键。
        var sharedConfigPath = Environment.GetEnvironmentVariable("GIRVS_SHARED_CONFIG");
        if (!string.IsNullOrWhiteSpace(sharedConfigPath))
            config.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);

        config.AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true);
```

注意:`AddJsonFile` 传绝对路径时自动按根路径解析,K8s 挂载路径与 AppHost 注入路径都是绝对路径。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj --filter "FullyQualifiedName~SharedConfigSourceTests"`
Expected: 4 个用例全部 PASS

- [ ] **Step 5: 全量构建验证多目标框架**

Run: `dotnet build Girvs.slnx`
Expected: 构建成功(核心是 net8.0/net9.0/net10.0 多目标,`Environment.GetEnvironmentVariable` 与 `AddJsonFile` 三个框架均可用)

- [ ] **Step 6: 提交**

```bash
git add Girvs/GirvsHostBuilderManager.cs tests/Girvs.Aspire.Tests/SharedConfigSourceTests.cs
git commit -m "feat: 服务端支持 GIRVS_SHARED_CONFIG 低优先级共享配置源"
```

---

### Task 2: 回滚服务端三个模块的 Aspire 连接串覆盖分支

**Files:**
- Modify: `Girvs.Cache/Configuration/CacheConfig.cs:39-46`
- Modify: `Girvs.Cache/GirvsCacheModule.cs:25,37,49,79-87`
- Modify: `Girvs.EntityFrameworkCore/Configuration/IDataConnectionStringProvider.cs:15-32`
- Modify: `Girvs.EntityFrameworkCore/GirvsEntityFrameworkCoreModule.cs:10-14`
- Modify: `Girvs.EventBus/EventBusModule.cs:19-20`
- Delete: `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs`

**Interfaces:**
- Consumes: 各模块现有 `BuildConnectionString(Resource)`(不动)
- Produces: `DataConnectionStringProvider(IEnumerable<DataConnectionConfig>, IReadOnlyDictionary<string, Resource>)` —— 构造函数去掉 `IConfiguration` 参数;`CacheConfig` 不再有 `GetConnectionString` 方法

- [ ] **Step 1: 删除覆盖分支与旧测试**

`Girvs.Cache/Configuration/CacheConfig.cs`:整个删除 `GetConnectionString` 方法(39-46 行)及文件顶部不再需要的 using(如有)。

`Girvs.Cache/GirvsCacheModule.cs`:私有辅助方法 `GetConnectionString(CacheConfig, IConfiguration)` 改为不需要 `IConfiguration`:

```csharp
    private static string GetConnectionString(CacheConfig cacheConfig)
    {
        var cache = cacheConfig.DistributedCacheConfig;
        var resources = Singleton<AppSettings>.Instance.Resources;
        var resource = resources.TryGetValue(cache.ConnectionRef, out var value)
            ? value
            : throw new GirvsException($"Resources:{cache.ConnectionRef} 未配置");
        return cache.BuildConnectionString(resource);
    }
```

三处调用点(25、37、49 行)同步改为 `GetConnectionString(cacheConfig)`。

`Girvs.EntityFrameworkCore/Configuration/IDataConnectionStringProvider.cs`:构造函数去掉 `IConfiguration configuration` 参数,连接串一律由资源组装:

```csharp
    public DataConnectionStringProvider(
        IEnumerable<DataConnectionConfig> configurations,
        IReadOnlyDictionary<string, Resource> resources
    )
    {
        foreach (var config in configurations)
        {
            var master = GetResource(resources, config.ConnectionRef);
            var masterConnection = config.BuildConnectionString(master);
            var reads = config.ReadConnectionRefs
                .Select(reference => config.BuildConnectionString(GetResource(resources, reference)))
                .ToList();
            _connections[config.Name] = (masterConnection, reads);
        }
    }
```

`Girvs.EntityFrameworkCore/GirvsEntityFrameworkCoreModule.cs:10-14` 注册处同步去掉第三个实参 `configuration`。

`Girvs.EventBus/EventBusModule.cs:19-20` 改为:

```csharp
        var connStr = eventBusConfig.BuildPersistenceConnectionString(
            resources[eventBusConfig.PersistenceConnectionRef]);
```

删除测试文件:

```bash
git rm tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs
```

- [ ] **Step 2: 构建并跑全部测试**

Run: `dotnet build Girvs.slnx && dotnet test Girvs.slnx`
Expected: 构建成功;测试全部通过(`ModuleConnectionReferenceTests` 只测 `BuildConnectionString`,不受影响;若 `tests/Girvs.Aspire.Hosting.Tests` 中有测试引用被删的构造函数签名,在本步一并修正其调用为双参数形式)

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: 移除 Aspire 连接串覆盖分支,连接信息统一由 Resources 组装"
```

---

### Task 3: 删除 Girvs.Aspire.Hosting 旧自动推断机制

**Files:**
- Delete: `Girvs.Aspire.Hosting/DependsOnGraph.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsResourceRegistry.cs`
- Delete: `Girvs.Aspire.Hosting/Contributors/`(整个目录:`IGirvsResourceContributor.cs`、`CacheResourceContributor.cs`、`DatabaseResourceContributor.cs`、`EventBusResourceContributor.cs`)
- Delete: `Girvs.Aspire.Hosting/GirvsSharedConfiguration.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsSharedConfigurationExtensions.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsServiceAppSettings.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsProjectOptions.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsOrchestrationContext.cs`
- Delete: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`(Task 6 重建)
- Delete: `tests/Girvs.Aspire.Hosting.Tests/DependsOnGraphTests.cs`
- Delete: `tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigurationTests.cs`
- Delete: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectOrchestrationTests.cs`

**Interfaces:**
- Produces: `Girvs.Aspire.Hosting` 暂时无公共 API(空模块,保留 csproj/GlobalUsings),`ModuleConnectionReferenceTests` 与 `ConnectionResourceResolverTests` 保留(它们只测模块 `BuildConnectionString` 与 `AppSettings` 绑定,不依赖被删类型)

- [ ] **Step 1: 删除文件**

```bash
git rm Girvs.Aspire.Hosting/DependsOnGraph.cs \
       Girvs.Aspire.Hosting/GirvsResourceRegistry.cs \
       Girvs.Aspire.Hosting/GirvsSharedConfiguration.cs \
       Girvs.Aspire.Hosting/GirvsSharedConfigurationExtensions.cs \
       Girvs.Aspire.Hosting/GirvsServiceAppSettings.cs \
       Girvs.Aspire.Hosting/GirvsProjectOptions.cs \
       Girvs.Aspire.Hosting/GirvsOrchestrationContext.cs \
       Girvs.Aspire.Hosting/GirvsProjectExtensions.cs
git rm -r Girvs.Aspire.Hosting/Contributors
git rm tests/Girvs.Aspire.Hosting.Tests/DependsOnGraphTests.cs \
       tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigurationTests.cs \
       tests/Girvs.Aspire.Hosting.Tests/GirvsProjectOrchestrationTests.cs
```

- [ ] **Step 2: 构建与测试**

Run: `dotnet build Girvs.slnx && dotnet test Girvs.slnx`
Expected: 通过。若 `Girvs.Aspire.Hosting/GlobalUsings.cs` 中有仅被已删文件使用的 using 导致警告,清理之;若保留的两个测试文件引用了被删类型,按编译错误移除对应用例(预期不会——它们只用 `Girvs.*.Configuration` 类型)

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: 删除 AppHost 端 DependsOn 自动推断与共享配置注入机制"
```

---

### Task 4: AsGirvsResource 登记扩展与资源条目生成

**Files:**
- Create: `Girvs.Aspire.Hosting/GirvsResourceAnnotation.cs`
- Create: `Girvs.Aspire.Hosting/GirvsResourceExtensions.cs`
- Create: `Girvs.Aspire.Hosting/GirvsResourceEntryBuilder.cs`
- Modify: `Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj`(新增 Kafka 包)
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsResourceEntryBuilderTests.cs`(新建)

**Interfaces:**
- Consumes: Aspire 13.4.6 资源类型(`RedisResource`、`MySqlServerResource`/`MySqlDatabaseResource`、`SqlServerServerResource`/`SqlServerDatabaseResource`、`RabbitMQServerResource`、`KafkaServerResource`)、`Girvs.Configuration.Resources.Resource`
- Produces:
  - `AsGirvsResource<T>(this IResourceBuilder<T> builder, string type = null, IDictionary<string, string> settings = null) where T : IResource` —— 登记扩展
  - `GirvsResourceAnnotation`(`string TypeOverride`、`IReadOnlyDictionary<string, string> ExtraSettings`)
  - `internal static Task<KeyValuePair<string, Resource>> GirvsResourceEntryBuilder.BuildAsync(IResource resource, GirvsResourceAnnotation annotation, CancellationToken ct)` —— Task 6 的文件生成依赖此方法

- [ ] **Step 1: csproj 加 Kafka 包**

`Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj` 的 PackageReference 组内新增:

```xml
        <PackageReference Include="Aspire.Hosting.Kafka" Version="13.4.6"/>
```

- [ ] **Step 2: 写失败测试**

新建 `tests/Girvs.Aspire.Hosting.Tests/GirvsResourceEntryBuilderTests.cs`:

```csharp
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsResourceEntryBuilderTests
{
    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true });

    /// <summary>为资源的主 endpoint 手动设置分配结果,模拟运行期端口分配。</summary>
    private static void AllocateEndpoint(IResource resource, int port)
    {
        var endpoint = resource.Annotations.OfType<EndpointAnnotation>().First();
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", port);
    }

    private static GirvsResourceAnnotation GetAnnotation(IResource resource) =>
        resource.Annotations.OfType<GirvsResourceAnnotation>().Single();

    [Fact]
    public async Task Redis资源生成Endpoints设置()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        AllocateEndpoint(redis.Resource, 56379);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("platform-redis", entry.Key);
        Assert.Equal("redis", entry.Value.Type);
        Assert.Equal("localhost:56379", entry.Value.Settings["Endpoints"]);
    }

    [Fact]
    public async Task Type覆盖参数生效()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("sample-redis")
            .AsGirvsResource(type: "redis-synchronized-memory");
        AllocateEndpoint(redis.Resource, 56380);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("redis-synchronized-memory", entry.Value.Type);
    }

    [Fact]
    public async Task MySql数据库资源生成完整连接设置()
    {
        var builder = CreateBuilder();
        var password = builder.AddParameter("mysql-pwd", "p@ss", secret: true);
        var server = builder.AddMySql("mysql-server", password: password);
        var database = server.AddDatabase("sample-mysql", databaseName: "sample")
            .AsGirvsResource();
        AllocateEndpoint(server.Resource, 53306);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            database.Resource, GetAnnotation(database.Resource), CancellationToken.None);

        Assert.Equal("sample-mysql", entry.Key);
        Assert.Equal("mysql", entry.Value.Type);
        Assert.Equal("localhost", entry.Value.Settings["Host"]);
        Assert.Equal("53306", entry.Value.Settings["Port"]);
        Assert.Equal("root", entry.Value.Settings["UserName"]);
        Assert.Equal("p@ss", entry.Value.Settings["Password"]);
        Assert.Equal("sample", entry.Value.Settings["Database"]);
    }

    [Fact]
    public async Task RabbitMQ资源生成HostName设置()
    {
        var builder = CreateBuilder();
        var user = builder.AddParameter("mq-user", "guest");
        var pwd = builder.AddParameter("mq-pwd", "guest", secret: true);
        var rabbit = builder.AddRabbitMQ("sample-mq", userName: user, password: pwd)
            .AsGirvsResource();
        AllocateEndpoint(rabbit.Resource, 55672);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            rabbit.Resource, GetAnnotation(rabbit.Resource), CancellationToken.None);

        Assert.Equal("rabbitmq", entry.Value.Type);
        Assert.Equal("localhost", entry.Value.Settings["HostName"]);
        Assert.Equal("55672", entry.Value.Settings["Port"]);
        Assert.Equal("guest", entry.Value.Settings["UserName"]);
        Assert.Equal("guest", entry.Value.Settings["Password"]);
    }

    [Fact]
    public async Task 额外Settings覆盖生成值()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis")
            .AsGirvsResource(settings: new Dictionary<string, string> { ["Ssl"] = "true" });
        AllocateEndpoint(redis.Resource, 56381);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("true", entry.Value.Settings["Ssl"]);
    }

    [Fact]
    public async Task 不支持的资源类型且未显式指定Type时抛异常()
    {
        var builder = CreateBuilder();
        var project = builder.AddContainer("unknown", "busybox").AsGirvsResource();
        AllocateEndpoint(project.Resource, 50000); // AddContainer 默认无 endpoint,此行如抛错则移除

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GirvsResourceEntryBuilder.BuildAsync(
                project.Resource, GetAnnotation(project.Resource), CancellationToken.None));
    }
}
```

注意:`AddContainer` 默认没有 `EndpointAnnotation`,若 `AllocateEndpoint` 抛 `InvalidOperationException`(First 无元素),删除该行——异常用例不需要 endpoint。

- [ ] **Step 3: 运行测试确认编译失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsResourceEntryBuilderTests"`
Expected: 编译错误 —— `GirvsResourceAnnotation`、`AsGirvsResource`、`GirvsResourceEntryBuilder` 未定义

- [ ] **Step 4: 实现三个新文件**

`Girvs.Aspire.Hosting/GirvsResourceAnnotation.cs`:

```csharp
namespace Girvs.Aspire.Hosting;

/// <summary>
/// 标记该 Aspire 资源需登记为 Girvs 共享资源:
/// 容器地址确定后写入运行时共享配置文件的 Resources 节点,键名即资源名。
/// </summary>
public sealed class GirvsResourceAnnotation(
    string typeOverride,
    IReadOnlyDictionary<string, string> extraSettings
) : IResourceAnnotation
{
    /// <summary>覆盖自动推断的资源 Type(如 redis-synchronized-memory);null 表示按资源类型推断。</summary>
    public string TypeOverride { get; } = typeOverride;

    /// <summary>补充或覆盖自动生成的 Settings 键值(如 Ssl、Database)。</summary>
    public IReadOnlyDictionary<string, string> ExtraSettings { get; } =
        extraSettings ?? new Dictionary<string, string>();
}
```

`Girvs.Aspire.Hosting/GirvsResourceExtensions.cs`:

```csharp
namespace Girvs.Aspire.Hosting;

public static class GirvsResourceExtensions
{
    /// <summary>
    /// 把该资源登记为 Girvs 共享资源:资源名即服务端 ConnectionRef 引用的键,
    /// 容器地址在启动期写入运行时共享配置文件(obj/girvs.shared.runtime.json)分发给各 Girvs 服务。
    /// </summary>
    /// <param name="type">覆盖自动推断的资源 Type;不支持的资源类型必须显式指定。</param>
    /// <param name="settings">补充或覆盖自动生成的 Settings 键值。</param>
    public static IResourceBuilder<T> AsGirvsResource<T>(
        this IResourceBuilder<T> builder,
        string type = null,
        IDictionary<string, string> settings = null
    )
        where T : IResource
    {
        return builder.WithAnnotation(
            new GirvsResourceAnnotation(
                type,
                settings is null ? null : new Dictionary<string, string>(settings)
            )
        );
    }
}
```

`Girvs.Aspire.Hosting/GirvsResourceEntryBuilder.cs`:

```csharp
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 把已登记的 Aspire 资源转换为 Girvs Resource 条目。
/// Settings 键与各模块 BuildConnectionString 的消费约定对齐(见设计文档表格)。
/// </summary>
internal static class GirvsResourceEntryBuilder
{
    public static async Task<KeyValuePair<string, GirvsResource>> BuildAsync(
        IResource resource,
        GirvsResourceAnnotation annotation,
        CancellationToken ct
    )
    {
        var (type, settings) = resource switch
        {
            RedisResource redis => ("redis", await RedisSettingsAsync(redis, ct)),
            MySqlDatabaseResource mysqlDb => (
                "mysql",
                await MySqlSettingsAsync(mysqlDb.Parent, mysqlDb.DatabaseName, ct)
            ),
            MySqlServerResource mysql => ("mysql", await MySqlSettingsAsync(mysql, null, ct)),
            SqlServerDatabaseResource sqlDb => (
                "sqlserver",
                await SqlServerSettingsAsync(sqlDb.Parent, sqlDb.DatabaseName, ct)
            ),
            SqlServerServerResource sql => (
                "sqlserver",
                await SqlServerSettingsAsync(sql, null, ct)
            ),
            RabbitMQServerResource rabbit => ("rabbitmq", await RabbitSettingsAsync(rabbit, ct)),
            KafkaServerResource kafka => ("kafka", KafkaSettings(kafka)),
            _ when annotation.TypeOverride is not null => (annotation.TypeOverride, new Dictionary<string, string>()),
            _ => throw new InvalidOperationException(
                $"AsGirvsResource 不支持资源类型 {resource.GetType().Name},请显式指定 type 与 settings 参数"
            ),
        };

        // 显式参数覆盖自动生成的结果
        if (annotation.TypeOverride is not null)
            type = annotation.TypeOverride;
        foreach (var (key, value) in annotation.ExtraSettings)
            settings[key] = value;

        return new KeyValuePair<string, GirvsResource>(
            resource.Name,
            new GirvsResource { Type = type, Settings = settings }
        );
    }

    private static (string Host, int Port) PrimaryEndpoint(IResourceWithEndpoints resource)
    {
        var endpoint = resource.GetEndpoints().First();
        return (endpoint.Host, endpoint.Port);
    }

    private static async Task<Dictionary<string, string>> RedisSettingsAsync(
        RedisResource redis,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(redis);
        var endpoints = $"{host}:{port}";
        // DistributedCacheConfig.BuildRedisConnectionString 会把 Endpoints 按逗号拆分后原样拼回,
        // 所以密码以 StackExchange.Redis 选项形式附在 Endpoints 内即可透传,模块无需感知。
        if (redis.PasswordParameter is not null)
            endpoints += $",password={await redis.PasswordParameter.GetValueAsync(ct)}";
        return new Dictionary<string, string> { ["Endpoints"] = endpoints };
    }

    private static async Task<Dictionary<string, string>> MySqlSettingsAsync(
        MySqlServerResource mysql,
        string databaseName,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(mysql);
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "root",
            ["Password"] = await mysql.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return settings;
    }

    private static async Task<Dictionary<string, string>> SqlServerSettingsAsync(
        SqlServerServerResource sql,
        string databaseName,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(sql);
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "sa",
            ["Password"] = await sql.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return settings;
    }

    private static async Task<Dictionary<string, string>> RabbitSettingsAsync(
        RabbitMQServerResource rabbit,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(rabbit);
        return new Dictionary<string, string>
        {
            ["HostName"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = rabbit.UserNameParameter is null
                ? "guest"
                : await rabbit.UserNameParameter.GetValueAsync(ct),
            ["Password"] = await rabbit.PasswordParameter.GetValueAsync(ct),
            ["VirtualHost"] = "/",
        };
    }

    private static Dictionary<string, string> KafkaSettings(KafkaServerResource kafka)
    {
        var (host, port) = PrimaryEndpoint(kafka);
        return new Dictionary<string, string> { ["BootstrapServers"] = $"{host}:{port}" };
    }
}
```

Aspire 13.4 API 校准点(编译期即可发现,按编译器提示微调,不改变行为契约):
- `ParameterResource.GetValueAsync(CancellationToken)` 若不存在,改用 `parameter.Value`;
- `EndpointReference.Host` / `.Port` 要求 endpoint 已分配,运行期由 WaitFor 保证,测试里手动分配;
- `RabbitMQServerResource.UserNameParameter` / `PasswordParameter`、`MySqlServerResource.PasswordParameter`、`SqlServerServerResource.PasswordParameter`、`RedisResource.PasswordParameter` 属性名如有出入以元数据为准;
- `GetEndpoints()` 来自 `IResourceWithEndpoints`,若个别资源类型未实现,用 `resource.Annotations.OfType<EndpointAnnotation>()` 取 `AllocatedEndpoint`。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsResourceEntryBuilderTests"`
Expected: 6 个用例全部 PASS

- [ ] **Step 6: 提交**

```bash
git add Girvs.Aspire.Hosting/ tests/Girvs.Aspire.Hosting.Tests/GirvsResourceEntryBuilderTests.cs
git commit -m "feat: AsGirvsResource 显式登记 Aspire 资源并生成 Girvs Resource 条目"
```

---

### Task 5: 共享配置合并器 GirvsSharedConfigMerger

**Files:**
- Create: `Girvs.Aspire.Hosting/GirvsSharedConfigMerger.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigMergerTests.cs`(新建)

**Interfaces:**
- Consumes: `Girvs.Configuration.Resources.Resource`
- Produces: `internal static string GirvsSharedConfigMerger.Merge(string handWrittenJson, IEnumerable<KeyValuePair<string, Resource>> entries)` —— 输入手写共享文件 JSON 文本(可 null)与登记资源条目,输出合并后 JSON 文本;Task 6 依赖

- [ ] **Step 1: 写失败测试**

新建 `tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigMergerTests.cs`:

```csharp
using System.Text.Json;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsSharedConfigMergerTests
{
    private static KeyValuePair<string, GirvsResource> Entry(
        string name, string type, params (string Key, string Value)[] settings) =>
        new(name, new GirvsResource
        {
            Type = type,
            Settings = settings.ToDictionary(x => x.Key, x => x.Value),
        });

    [Fact]
    public void 无手写文件时生成仅含登记资源的文档()
    {
        var json = GirvsSharedConfigMerger.Merge(null,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        var resource = doc.RootElement.GetProperty("Resources").GetProperty("platform-redis");
        Assert.Equal("redis", resource.GetProperty("Type").GetString());
        Assert.Equal("localhost:56379",
            resource.GetProperty("Settings").GetProperty("Endpoints").GetString());
    }

    [Fact]
    public void 手写文件的非Resources节点原样保留()
    {
        const string handWritten =
            """{"Logging":{"LogLevel":{"Default":"Warning"}},"Resources":{}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Warning", doc.RootElement
            .GetProperty("Logging").GetProperty("LogLevel").GetProperty("Default").GetString());
        Assert.True(doc.RootElement.GetProperty("Resources").TryGetProperty("platform-redis", out _));
    }

    [Fact]
    public void 登记资源覆盖手写文件中的同名资源()
    {
        const string handWritten =
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"prod:6379"}}}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("localhost:56379", doc.RootElement.GetProperty("Resources")
            .GetProperty("platform-redis").GetProperty("Settings").GetProperty("Endpoints").GetString());
    }

    [Fact]
    public void 手写文件中未登记的资源原样透传()
    {
        const string handWritten =
            """{"Resources":{"legacy-redis":{"Type":"redis","Settings":{"Endpoints":"10.0.0.8:6379"}}}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        var resources = doc.RootElement.GetProperty("Resources");
        Assert.Equal("10.0.0.8:6379", resources.GetProperty("legacy-redis")
            .GetProperty("Settings").GetProperty("Endpoints").GetString());
        Assert.True(resources.TryGetProperty("platform-redis", out _));
    }
}
```

- [ ] **Step 2: 运行测试确认编译失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsSharedConfigMergerTests"`
Expected: 编译错误 —— `GirvsSharedConfigMerger` 未定义

- [ ] **Step 3: 实现**

新建 `Girvs.Aspire.Hosting/GirvsSharedConfigMerger.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 合并手写共享配置(girvs.shared.json)与 AsGirvsResource 登记的资源条目:
/// 手写内容整体保留,登记资源覆盖 Resources 下同名键。输出运行时共享文件的 JSON 文本。
/// </summary>
internal static class GirvsSharedConfigMerger
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string Merge(
        string handWrittenJson,
        IEnumerable<KeyValuePair<string, GirvsResource>> entries
    )
    {
        var root = string.IsNullOrWhiteSpace(handWrittenJson)
            ? new JsonObject()
            : JsonNode.Parse(handWrittenJson)!.AsObject();

        if (root["Resources"] is not JsonObject resources)
        {
            resources = new JsonObject();
            root["Resources"] = resources;
        }

        foreach (var (name, resource) in entries)
        {
            var settings = new JsonObject();
            foreach (var (key, value) in resource.Settings)
                settings[key] = value;
            resources[name] = new JsonObject
            {
                ["Type"] = resource.Type,
                ["Settings"] = settings,
            };
        }

        return root.ToJsonString(WriteOptions);
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsSharedConfigMergerTests"`
Expected: 4 个用例全部 PASS

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire.Hosting/GirvsSharedConfigMerger.cs tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigMergerTests.cs
git commit -m "feat: 共享配置合并器,手写文件与登记资源合并生成运行时文档"
```

---

### Task 6: 新 AddGirvsProject 与运行时共享文件生成

**Files:**
- Create: `Girvs.Aspire.Hosting/GirvsSharedConfigFile.cs`
- Create: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`(重建)
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`(新建)

**Interfaces:**
- Consumes: `GirvsResourceEntryBuilder.BuildAsync`(Task 4)、`GirvsSharedConfigMerger.Merge`(Task 5)
- Produces:
  - `AddGirvsProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()` —— 公共 API,samples 与业务方使用
  - `internal AddGirvsProject(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> project)` 接线核心(便于测试注入任意 project)
  - 服务进程环境变量 `GIRVS_SHARED_CONFIG`(run:运行时文件绝对路径;publish:`/girvs-config/girvs.shared.json`)

- [ ] **Step 1: 写失败测试**

新建 `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`:

```csharp
namespace Girvs.Aspire.Hosting.Tests;

public class GirvsProjectExtensionsTests : IDisposable
{
    private readonly string _tempRoot = Directory
        .CreateTempSubdirectory("girvs-project-extensions-tests")
        .FullName;

    public void Dispose() => Directory.Delete(_tempRoot, true);

    private IDistributedApplicationBuilder CreateBuilder(bool publish = false) =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions
            {
                DisableDashboard = true,
                ProjectDirectory = _tempRoot,
                Args = publish
                    ? ["--operation", "publish", "--publisher", "manifest", "--output-path", "."]
                    : [],
            });

    private IResourceBuilder<ProjectResource> AddServiceProject(
        IDistributedApplicationBuilder builder, string name)
    {
        var csproj = Path.Combine(_tempRoot, $"{name}.csproj");
        File.WriteAllText(csproj, """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>""");
        return builder.AddProject(name, csproj);
    }

    private static void AllocateEndpoint(IResource resource, int port)
    {
        var endpoint = resource.Annotations.OfType<EndpointAnnotation>().First();
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", port);
    }

    [Fact]
    public void 服务对已登记资源自动WaitFor()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var waits = project.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waits, w => w.Resource == redis.Resource);
    }

    [Fact]
    public async Task run模式注入运行时共享文件路径且文件内容含登记资源()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        AllocateEndpoint(redis.Resource, 56379);
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        var path = Assert.Contains("GIRVS_SHARED_CONFIG", env);
        Assert.EndsWith(Path.Combine("obj", "girvs.shared.runtime.json"), path);
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("platform-redis", content);
        Assert.Contains("localhost:56379", content);
    }

    [Fact]
    public async Task run模式合并AppHost目录手写共享文件()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "girvs.shared.json"),
            """{"Logging":{"LogLevel":{"Default":"Warning"}}}""");
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        var content = await File.ReadAllTextAsync(env["GIRVS_SHARED_CONFIG"]);
        Assert.Contains("Warning", content);
    }

    [Fact]
    public async Task publish模式注入约定挂载路径且不生成文件()
    {
        var builder = CreateBuilder(publish: true);
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish);

        Assert.Equal("/girvs-config/girvs.shared.json", env["GIRVS_SHARED_CONFIG"]);
        Assert.False(File.Exists(
            Path.Combine(_tempRoot, "obj", "girvs.shared.runtime.json")));
    }
}
```

Aspire 13.4 API 校准点:`DistributedApplicationOptions.ProjectDirectory` 用于把 `builder.AppHostDirectory` 指到临时目录;`GetEnvironmentVariableValuesAsync` 是 `Aspire.Hosting.ApplicationModel.ResourceExtensions` 的测试辅助扩展。若属性/方法名有出入,以包元数据为准调整,断言意图不变。

- [ ] **Step 2: 运行测试确认编译失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsProjectExtensionsTests"`
Expected: 编译错误 —— `AddGirvsProject` 未定义

- [ ] **Step 3: 实现**

新建 `Girvs.Aspire.Hosting/GirvsSharedConfigFile.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 运行时共享文件的幂等生成:首个服务的环境回调触发写入,后续服务复用同一结果。
/// 生成时机在服务启动前(环境回调求值时),此时被 WaitFor 的容器 endpoint 均已分配。
/// </summary>
internal static class GirvsSharedConfigFile
{
    public const string EnvName = "GIRVS_SHARED_CONFIG";
    public const string HandWrittenFileName = "girvs.shared.json";
    public const string PublishMountPath = "/girvs-config/girvs.shared.json";

    // 按 builder 实例缓存生成任务:AppHost 生命周期内只写一次,多服务并发回调安全。
    private static readonly ConditionalWeakTable<
        IDistributedApplicationBuilder,
        Lazy<Task<string>>
    > Cache = new();

    public static Task<string> EnsureWrittenAsync(
        IDistributedApplicationBuilder builder,
        CancellationToken ct
    )
    {
        var lazy = Cache.GetValue(
            builder,
            b => new Lazy<Task<string>>(() => WriteAsync(b, ct))
        );
        return lazy.Value;
    }

    private static async Task<string> WriteAsync(
        IDistributedApplicationBuilder builder,
        CancellationToken ct
    )
    {
        var entries = new List<KeyValuePair<string, Girvs.Configuration.Resources.Resource>>();
        foreach (var resource in builder.Resources)
        {
            var annotation = resource.Annotations.OfType<GirvsResourceAnnotation>().LastOrDefault();
            if (annotation is null)
                continue;
            entries.Add(await GirvsResourceEntryBuilder.BuildAsync(resource, annotation, ct));
        }

        var handWrittenPath = Path.Combine(builder.AppHostDirectory, HandWrittenFileName);
        var handWrittenJson = File.Exists(handWrittenPath)
            ? await File.ReadAllTextAsync(handWrittenPath, ct)
            : null;

        var merged = GirvsSharedConfigMerger.Merge(handWrittenJson, entries);
        var outputPath = Path.Combine(
            builder.AppHostDirectory,
            "obj",
            "girvs.shared.runtime.json"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, merged, ct);
        return outputPath;
    }
}
```

新建 `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`:

```csharp
namespace Girvs.Aspire.Hosting;

public static class GirvsProjectExtensions
{
    /// <summary>
    /// 添加 Girvs 服务项目:注入 GIRVS_SHARED_CONFIG 共享配置文件路径,
    /// 并对所有已通过 AsGirvsResource 登记的资源 WaitFor。
    /// 约定:先编排基础资源(AsGirvsResource),再 AddGirvsProject。
    /// 服务用不用某资源、用哪个,由服务自己配置中的 ConnectionRef 决定。
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name
    )
        where TProject : IProjectMetadata, new()
    {
        return builder.AddGirvsProject(builder.AddProject<TProject>(name));
    }

    internal static IResourceBuilder<ProjectResource> AddGirvsProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> project
    )
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            // 生产:共享文件由 K8s ConfigMap 挂载到约定路径,内容与地址由运维维护
            project.WithEnvironment(
                GirvsSharedConfigFile.EnvName,
                GirvsSharedConfigFile.PublishMountPath
            );
            return project;
        }

        foreach (var resource in builder.Resources)
        {
            if (resource.Annotations.OfType<GirvsResourceAnnotation>().Any())
                project.WaitFor(builder.CreateResourceBuilder(resource));
        }

        // 环境回调在服务启动前求值,此时 WaitFor 的容器 endpoint 已分配,可安全写文件
        project.WithEnvironment(async context =>
        {
            var path = await GirvsSharedConfigFile.EnsureWrittenAsync(
                builder,
                context.CancellationToken
            );
            context.EnvironmentVariables[GirvsSharedConfigFile.EnvName] = path;
        });

        return project;
    }
}
```

Aspire 13.4 API 校准点:`WaitFor` 要求依赖参数为 `IResourceBuilder<IResource>`,`builder.CreateResourceBuilder(resource)` 满足;`WithEnvironment(Func<EnvironmentCallbackContext, Task>)` 异步重载存在于 13.x;`EnvironmentCallbackContext.CancellationToken` 如无此属性用 `CancellationToken.None`。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 全部 PASS(含 Task 4/5 的用例)

- [ ] **Step 5: 全量构建与测试**

Run: `dotnet build Girvs.slnx && dotnet test Girvs.slnx`
Expected: 通过

- [ ] **Step 6: 提交**

```bash
git add Girvs.Aspire.Hosting/ tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs
git commit -m "feat: AddGirvsProject 注入共享配置文件并生成运行时资源地址"
```

---

### Task 7: samples 迁移到新机制

**Files:**
- Modify: `samples/Sample.AppHost/Program.cs`
- Create: `samples/Sample.AppHost/girvs.shared.json`
- Modify: `samples/Sample.AppHost/Sample.AppHost.csproj`(如需把 girvs.shared.json 设为 `CopyToOutputDirectory=Never`,默认不需要)
- Modify: `samples/Sample.ServiceA/appsettings.json`
- Modify: `samples/Sample.ServiceB/appsettings.json`
- Modify: `samples/Sample.Worker/appsettings.json`

**Interfaces:**
- Consumes: `AddGirvsProject<TProject>(name)`、`AsGirvsResource(type, settings)`(Task 4/6)
- Produces: 可运行的端到端参照样例

- [ ] **Step 1: 重写 AppHost Program.cs**

`samples/Sample.AppHost/Program.cs` 全文替换:

```csharp
using Girvs.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// 显式编排基础资源并登记为 Girvs 资源:资源名即各服务 ConnectionRef 引用的键。
// 服务用不用、用哪个,由服务自己的 appsettings 决定,AppHost 不感知。
var redis = builder.AddRedis("sample-redis").AsGirvsResource(type: "redis-synchronized-memory");
var mysql = builder.AddMySql("sample-mysql-server").AddDatabase("sample-mysql", databaseName: "sample").AsGirvsResource();
var rabbit = builder.AddRabbitMQ("sample-rabbitmq").AsGirvsResource();

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b");

// service-a 引用 service-b:注入其发现地址,供 service-a 用 http://service-b 通过服务发现调用
builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a").WithReference(serviceB);

// 后台工作服务:AddGirvsProject 对含 BackgroundService 的服务同样适用
builder.AddGirvsProject<Projects.Sample_Worker>("worker");

builder.Build().Run();
```

- [ ] **Step 2: 新建手写共享文件**

`samples/Sample.AppHost/girvs.shared.json`(演示非资源共享配置与合并透传):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- [ ] **Step 3: 迁移三个服务的 appsettings.json**

`samples/Sample.ServiceA/appsettings.json` 中 `ModuleConfigurations` 改为新 schema(保留文件中 `CommonConfig`/`HostingConfig` 原样):

```json
  "ModuleConfigurations": {
    "CacheConfig": {
      "EnableCaching": true,
      "DefaultCacheTime": 60,
      "DistributedCacheConfig": {
        "Enabled": true,
        "ConnectionRef": "sample-redis",
        "InstanceName": "sample-a",
        "PublishIntervalMs": 500
      }
    },
    "DbConfig": {
      "DataConnectionConfigs": [
        {
          "Name": "default",
          "EnableAutoMigrate": false,
          "UseDataType": 1,
          "SQLCommandTimeout": 30,
          "UseDataTracking": true,
          "ConnectionRef": "sample-mysql",
          "ReadConnectionRefs": []
        }
      ]
    }
  }
```

`samples/Sample.ServiceB/appsettings.json` 的 `EventBusConfig` 改为新 schema(对齐 `EventBusConfig.cs` 的 `PersistenceConnectionRef`/`TransportConnectionRef`):

```json
    "EventBusConfig": {
      "PersistenceConnectionRef": "sample-mysql",
      "TransportConnectionRef": "sample-rabbitmq",
      "ConsumerThreadCount": 1,
      "ProducerThreadCount": 1,
      "SucceedMessageExpiredAfter": 60,
      "FailedMessageExpiredAfter": 1296000
    }
```

`DbConfig` 部分与 ServiceA 相同处理(`ConnectionRef: "sample-mysql"`)。ServiceB 若有 `CacheConfig` 一并对齐 `ConnectionRef: "sample-redis"`。

`samples/Sample.Worker/appsettings.json`:已是新 schema,仅删除其中的本地 `Resources` 节点(资源地址改由共享文件提供),`ConnectionRef: "sample-redis"` 保持。若希望 Worker 支持脱离 AppHost 独立运行,可保留该节点作为本地回退——默认删除,保持样例语义单一。

迁移时注意:各服务 appsettings 中旧字段(`ConnectionString`、`MasterDataConnectionString`、`DistributedCacheType`、`DbConnectionString`、`RabbitMqConfig`、`KafkaConfig`、`RedisConfig` 等)全部删除,它们已不被新配置模型消费。

- [ ] **Step 4: 构建样例解决方案**

Run: `dotnet build samples/GirvsAspireSample.slnx`
Expected: 构建成功

- [ ] **Step 5: 端到端运行验证(需本机 Docker)**

Run: `no_proxy=localhost,127.0.0.1,::1 dotnet run --project samples/Sample.AppHost`(后台运行,观察 30-60 秒日志后停止)
Expected:
- AppHost 目录出现 `obj/girvs.shared.runtime.json`,内容含 `sample-redis`/`sample-mysql`/`sample-rabbitmq` 三个资源与实际端口;
- 各服务启动无 `Resources:* 未配置` 异常;
- 访问 `service-a` 的 `/selfcheck/cache` 等自检端点返回成功(端点见 `samples/README.md`)。

若无 Docker 环境,此步降级为:检查 `dotnet run` 启动日志中共享文件已生成、服务进程环境变量含 `GIRVS_SHARED_CONFIG`,并在提交信息中注明未做容器级验证。

- [ ] **Step 6: 提交**

```bash
git add samples/
git commit -m "refactor: 样例迁移到 AsGirvsResource 显式编排与共享配置文件分发"
```

---

### Task 8: 文档同步更新

**Files:**
- Modify: `Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj`(Description)
- Modify: `docs/aspire/apphost-guide.md`
- Modify: `samples/README.md`
- Modify: `CLAUDE.md`(模块表格 Girvs.Aspire.Hosting 行)
- Modify: `AGENTS.md`(如含同样的模块描述则同步)

**Interfaces:**
- Consumes: Task 1-7 落地的最终行为
- Produces: 与代码一致的文档

- [ ] **Step 1: 更新 csproj Description**

```xml
        <Description>Girvs 框架的 .NET Aspire AppHost 编排扩展:AsGirvsResource 显式登记基础资源,AddGirvsProject 生成并分发共享配置文件(GIRVS_SHARED_CONFIG)。</Description>
```

- [ ] **Step 2: 更新 apphost-guide.md**

重写其中 AppHost 接入章节,内容要点(用实际落地的 API 与路径):
- 三种部署形态的配置来源与优先级链(共享文件 < appsettings.json < appsettings.{Environment}.json < 环境变量);
- AppHost 用法示例(与 samples/Sample.AppHost/Program.cs 一致);
- `girvs.shared.json` 手写文件的定位与合并规则(登记资源覆盖同名键,其余透传);
- 生产 K8s:ConfigMap 挂载 `/girvs-config/girvs.shared.json`,`GIRVS_SHARED_CONFIG` 已由 publish 注入;
- 迁移说明:`AddGirvsSharedConfiguration`、`rootModuleType` 参数、`ConnectionStrings__girvs-*` 覆盖均已移除。

- [ ] **Step 3: 更新 CLAUDE.md 模块表格行与 samples/README.md**

CLAUDE.md 中 `Girvs.Aspire.Hosting` 行改为:

```
| `Girvs.Aspire.Hosting` | Aspire AppHost 编排扩展(仅 net10.0):`AsGirvsResource` 显式登记基础资源,`AddGirvsProject` 生成共享配置文件并注入 `GIRVS_SHARED_CONFIG` |
```

`Girvs.Aspire` 行中"连接串映射"字样删除。samples/README.md 与 AGENTS.md 中涉及旧机制([DependsOn] 自动资源、共享配置注入)的描述同步改写。

- [ ] **Step 4: 最终全量验证**

Run: `dotnet build Girvs.slnx && dotnet test Girvs.slnx && dotnet build samples/GirvsAspireSample.slnx`
Expected: 全部通过

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "docs: 同步共享配置文件分发机制文档"
```
