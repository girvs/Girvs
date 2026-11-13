---
workflow_status: completed
spec_path: docs/superpowers/specs/2026-08-03-serilog-code-sink-registration-design.md
approved_at: 2026-08-03
blocked_reason: null
---

# Girvs Serilog 代码方式 Sink 注册 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- []) syntax for tracking.

**Goal:** 新增 `services.AddSerilogSink()` 扩展方法，使业务可通过代码注册 Serilog Sink，并与 `appsettings.json` 配置方式共存，代码声明的 Sink 类型覆盖配置同名条目。

**Architecture:** 静态注册表 `GirvsSerilogSinkRegistry` 在 `ConfigureServices` 阶段收集委托，`HostUseSerilog` 回调在执行代码委托后构造裁剪版 `IConfiguration`（过滤被覆盖的 `WriteTo` 条目），再传给 `ReadFrom.Configuration` 处理非 WriteTo 配置，最后 Console 兜底。

**Tech Stack:** .NET 8/9/10、Serilog.AspNetCore、Serilog.Settings.Configuration、Serilog.Sinks.Console、xUnit、Serilog.Sinks.TestCorrelator。

## Global Constraints

- Girvs 核心不引用 `Serilog.Sinks.File`、`Serilog.Sinks.Elasticsearch`、`Serilog.Sinks.OpenTelemetry` 等具体生产 Sink 包。
- `HostUseSerilog` 的 Console 兜底模板保持 `{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}`，不含 `||end`。
- `ReadFrom.Configuration` 必须在 Console 默认配置之后、代码 Sink 执行之后调用。
- 代码委托执行异常时打 `SelfLog` 并跳过该委托，不抛异常阻止 App 启动。
- `AddSerilogSink` 的 `overriddenSinkTypeNames` 集合完全相等时后者替换前者。
- 不提交 Git commit，除非用户另行明确要求。

---

### Task 1: GirvsSerilogSinkRegistry 静态注册表

**Files:**
- Create: `Girvs/Infrastructure/GirvsSerilogSinkRegistry.cs`
- Create: `tests/Girvs.Core.Tests/GirvsSerilogSinkRegistryTests.cs`

**Interfaces:**
- Consumes: 无依赖。
- Produces: `GirvsSerilogSinkRegistry.Registrations`（`List<SerilogSinkRegistration>`）、`SnapshotAndClear() : List<SerilogSinkRegistration>`、`SerilogSinkRegistration` record。

- [ ] **Step 1: 写失败测试 — SnapshotAndClear 返回已注册的全部委托并清空注册表**

  ```csharp
  // tests/Girvs.Core.Tests/GirvsSerilogSinkRegistryTests.cs
  namespace Girvs.Core.Tests;

  public class GirvsSerilogSinkRegistryTests
  {
      [Fact]
      public void SnapshotAndClear_注册两个委托_返回两个并清空注册表()
      {
          var reg1 = new SerilogSinkRegistration(
              _ => { },
              new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "File" }
          );
          var reg2 = new SerilogSinkRegistration(
              _ => { },
              new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Elasticsearch" }
          );

          GirvsSerilogSinkRegistry.Registrations.Add(reg1);
          GirvsSerilogSinkRegistry.Registrations.Add(reg2);

          var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();

          Assert.Equal(2, snapshot.Count);
          Assert.Contains(reg1, snapshot);
          Assert.Contains(reg2, snapshot);
          Assert.Empty(GirvsSerilogSinkRegistry.Registrations);
      }

      [Fact]
      public void SnapshotAndClear_注册表已空_返回空列表()
      {
          var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();

          Assert.Empty(snapshot);
      }
  }
  ```

- [ ] **Step 2: 运行测试，确认 RED**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkRegistryTests"`
  Expected: FAIL — `GirvsSerilogSinkRegistry`、`SerilogSinkRegistration` 类型不存在。

- [ ] **Step 3: 实现最小代码**

  ```csharp
  // Girvs/Infrastructure/GirvsSerilogSinkRegistry.cs
  using Serilog;

  namespace Girvs;

  internal static class GirvsSerilogSinkRegistry
  {
      internal static readonly List<SerilogSinkRegistration> Registrations = [];

      internal static List<SerilogSinkRegistration> SnapshotAndClear()
      {
          var snapshot = new List<SerilogSinkRegistration>(Registrations);
          Registrations.Clear();
          return snapshot;
      }
  }

  internal sealed record SerilogSinkRegistration(
      Action<LoggerConfiguration> Configure,
      IReadOnlySet<string> OverriddenSinkTypeNames
  );
  ```

- [ ] **Step 4: 运行测试，确认 GREEN**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkRegistryTests"`
  Expected: PASS — 两项测试均通过。

- [ ] **Step 5: 在 Girvs.csproj 中声明 InternalsVisibleTo**

  在 `Girvs/Girvs.csproj` 中添加：

  ```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Girvs.Core.Tests" />
    <InternalsVisibleTo Include="Girvs.ServiceGovernance.Tests" />
    <InternalsVisibleTo Include="Sample.Gateway.Tests" />
  </ItemGroup>
  ```

- [ ] **Step 6: 运行既有测试确保无回归**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo`
  Expected: PASS — 既有 `GirvsHostBuilderManagerTests` 全部通过。

---

### Task 2: AddSerilogSink 扩展方法

**Files:**
- Create: `Girvs/Infrastructure/Extensions/SerilogSinkServiceCollectionExtensions.cs`
- Modify: `tests/Girvs.Core.Tests/GirvsSerilogSinkRegistryTests.cs`（追加测试）

**Interfaces:**
- Consumes: `GirvsSerilogSinkRegistry.Registrations`。
- Produces: `SerilogSinkServiceCollectionExtensions.AddSerilogSink(IServiceCollection, Action<LoggerConfiguration>, params string[]) : IServiceCollection`。

- [ ] **Step 1: 追加失败测试**

  在 `GirvsSerilogSinkRegistryTests` 末尾追加：

  ```csharp
  [Fact]
  public void AddSerilogSink_注册一个委托_写入注册表()
  {
      var services = new ServiceCollection();
      bool called = false;
      services.AddSerilogSink(_ => called = true, "File");

      var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();
      Assert.Single(snapshot);
      var reg = snapshot[0];
      Assert.True(reg.OverriddenSinkTypeNames.SetEquals(["File"]));
      reg.Configure(null!);
      Assert.True(called);
  }

  [Fact]
  public void AddSerilogSink_相同覆盖集合注册两次_后者替换前者()
  {
      var services = new ServiceCollection();
      bool firstCalled = false, secondCalled = false;
      services.AddSerilogSink(_ => firstCalled = true, "File");
      services.AddSerilogSink(_ => secondCalled = true, "File");

      var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();
      Assert.Single(snapshot);
      snapshot[0].Configure(null!);
      Assert.False(firstCalled);
      Assert.True(secondCalled);
  }

  [Fact]
  public void AddSerilogSink_不同覆盖集合注册_两者皆保留()
  {
      var services = new ServiceCollection();
      services.AddSerilogSink(_ => { }, "File");
      services.AddSerilogSink(_ => { }, "Elasticsearch");

      var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();
      Assert.Equal(2, snapshot.Count);
  }
  ```

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkRegistryTests"`
  Expected: FAIL — `IServiceCollection` 上不存在 `AddSerilogSink` 扩展方法。

- [ ] **Step 2: 实现最小代码**

  ```csharp
  // Girvs/Infrastructure/Extensions/SerilogSinkServiceCollectionExtensions.cs
  using Microsoft.Extensions.DependencyInjection;
  using Serilog;

  namespace Girvs;

  public static class SerilogSinkServiceCollectionExtensions
  {
      public static IServiceCollection AddSerilogSink(
          this IServiceCollection services,
          Action<LoggerConfiguration> configure,
          params string[] overriddenSinkTypeNames)
      {
          ArgumentNullException.ThrowIfNull(configure);

          var newOverrides = overriddenSinkTypeNames
              .ToHashSet(StringComparer.OrdinalIgnoreCase);

          GirvsSerilogSinkRegistry.Registrations.RemoveAll(r =>
              r.OverriddenSinkTypeNames.SetEquals(newOverrides));

          GirvsSerilogSinkRegistry.Registrations.Add(
              new SerilogSinkRegistration(configure, newOverrides));

          return services;
      }
  }
  ```

- [ ] **Step 3: 运行测试，确认 GREEN**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkRegistryTests"`
  Expected: PASS — 全部 5 项测试通过。

---

### Task 3: HostUseSerilog 改造 + BuildFilteredSerilogSection

**Files:**
- Modify: `Girvs/GirvsHostBuilderManager.cs`
- Modify: `tests/Girvs.Core.Tests/GirvsHostBuilderManagerTests.cs`（追加集成测试）

**Interfaces:**
- Consumes: `GirvsSerilogSinkRegistry.SnapshotAndClear()`、`SerilogSinkRegistration.Configure/OverriddenSinkTypeNames`。
- Produces: 改造后 `HostUseSerilog`、`BuildFilteredSerilogSection` 私有方法。

- [ ] **Step 1: 追加失败集成测试**

  在 `GirvsHostBuilderManagerTests` 末尾（class 内部）追加：

  ```csharp
  [Fact]
  public void HostUseSerilog_代码注册Sink且未覆盖TestCorrelator配置_TestCorrelator仍接收日志()
  {
      var hostBuilder = new HostBuilder()
          .ConfigureAppConfiguration((_, config) =>
          {
              config.Sources.Clear();
              config.AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Serilog:MinimumLevel:Default"] = "Debug",
                  ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
              });
          });

      // 代码注册 File 覆盖，但 TestCorrelator 未覆盖 → 配置的 TestCorrelator 应保留
      var services = new ServiceCollection();
      services.AddSerilogSink(_ => { }, "File");

      hostBuilder.HostUseSerilog();
      var host = hostBuilder.Build();

      using (host)
      using (TestCorrelator.CreateContext())
      {
          host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
              .LogInformation("配置 TestCorrelator 应接收");
          Assert.Contains(
              TestCorrelator.GetLogEventsFromCurrentContext(),
              le => le.MessageTemplate.Text == "配置 TestCorrelator 应接收");
      }
  }

  [Fact]
  public void HostUseSerilog_代码覆盖TestCorrelator_配置的TestCorrelator被跳过()
  {
      var hostBuilder = new HostBuilder()
          .ConfigureAppConfiguration((_, config) =>
          {
              config.Sources.Clear();
              config.AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Serilog:MinimumLevel:Default"] = "Debug",
                  ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
              });
          });

      bool codeSinkCalled = false;
      var services = new ServiceCollection();
      services.AddSerilogSink(_ => codeSinkCalled = true, "TestCorrelator");

      hostBuilder.HostUseSerilog();
      var host = hostBuilder.Build();

      using (host)
      using (TestCorrelator.CreateContext())
      {
          host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
              .LogInformation("代码 sink 应处理");

          // 代码 sink 被调用
          Assert.True(codeSinkCalled);

          // 配置中的 TestCorrelator 被覆盖 → 不应收到日志
          Assert.DoesNotContain(
              TestCorrelator.GetLogEventsFromCurrentContext(),
              le => le.MessageTemplate.Text == "代码 sink 应处理");
      }
  }

  [Fact]
  public void HostUseSerilog_代码委托抛异常_App不崩溃且Console兜底有效()
  {
      var hostBuilder = new HostBuilder()
          .ConfigureAppConfiguration((_, config) =>
          {
              config.Sources.Clear();
              config.AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Environment"] = "Test",
              });
          });

      var services = new ServiceCollection();
      services.AddSerilogSink(
          _ => throw new InvalidOperationException("模拟 Sink 注册失败"),
          "File");

      hostBuilder.HostUseSerilog();
      var host = hostBuilder.Build();

      using (host)
      {
          host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
              .LogInformation("兜底 Console 仍可用");
          // 不抛异常即通过
      }
  }
  ```

  注意：需要将 class 的 `[Collection("ConsoleSink")]` 属性移除（新测试不依赖 Console.SetOut），或为新测试创建独立测试类。  **推荐创建独立的 `GirvsSerilogSinkIntegrationTests` 类**，不标记 Collection，使用 TestCorrelator 验证。每个测试开头调用 `GirvsSerilogSinkRegistry.Registrations.Clear()` 确保静态注册表隔离。

  ```csharp
  // tests/Girvs.Core.Tests/GirvsSerilogSinkIntegrationTests.cs
  public class GirvsSerilogSinkIntegrationTests
  {
      public GirvsSerilogSinkIntegrationTests()
      {
          GirvsSerilogSinkRegistry.Registrations.Clear();
      }
      // ... 三个测试方法
  }
  ```

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkIntegrationTests"`
  Expected: FAIL — `HostUseSerilog` 尚未消费注册表。

- [ ] **Step 2: 改造 HostUseSerilog**

  ```csharp
  // Girvs/GirvsHostBuilderManager.cs — 替换 HostUseSerilog 方法体
  using Serilog.Debugging;

  public static void HostUseSerilog(this IHostBuilder hostBuilder)
  {
      hostBuilder.UseSerilog((context, configuration) =>
      {
          configuration
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("System", LogEventLevel.Warning)
              .WriteTo.Console(
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}"
              );

          var registrations = GirvsSerilogSinkRegistry.SnapshotAndClear();
          var overrides = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          foreach (var reg in registrations)
          {
              try
              {
                  reg.Configure(configuration);
              }
              catch (Exception ex)
              {
                  SelfLog.WriteLine(
                      "Girvs Serilog Sink 注册失败 [{0}]: {1}",
                      string.Join(",", reg.OverriddenSinkTypeNames), ex);
              }
              foreach (var name in reg.OverriddenSinkTypeNames)
                  overrides.Add(name);
          }

          var serilogSection = context.Configuration.GetSection("Serilog");
          if (serilogSection.Exists())
          {
              var filteredConfig = BuildFilteredSerilogSection(serilogSection, overrides);
              configuration.ReadFrom.Configuration(filteredConfig);
          }
      });
  }
  ```

- [ ] **Step 3: 实现 BuildFilteredSerilogSection**

  在 `GirvsHostBuilderManager` 类内追加私有方法：

  ```csharp
  private static IConfiguration BuildFilteredSerilogSection(
      IConfigurationSection serilogSection,
      HashSet<string> overrides)
  {
      var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

      foreach (var child in serilogSection.GetChildren())
      {
          if (string.Equals(child.Key, "WriteTo", StringComparison.OrdinalIgnoreCase))
          {
              var writeToIndex = 0;
              foreach (var sinkItem in child.GetChildren())
              {
                  var sinkName = sinkItem["Name"];
                  if (!string.IsNullOrEmpty(sinkName) && overrides.Contains(sinkName))
                      continue;

                  foreach (var prop in sinkItem.GetChildren())
                      FlattenConfigSection(prop, $"Serilog:WriteTo:{writeToIndex}:{prop.Key}", data);
                  writeToIndex++;
              }
          }
          else
          {
              foreach (var sub in child.GetChildren())
                  FlattenConfigSection(sub, $"Serilog:{child.Key}:{sub.Key}", data);
          }
      }

      return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
  }

  private static void FlattenConfigSection(
      IConfigurationSection section,
      string parentPath,
      Dictionary<string, string?> data)
  {
      if (!section.GetChildren().Any())
          data[parentPath] = section.Value;
      else
          foreach (var child in section.GetChildren())
              FlattenConfigSection(child, $"{parentPath}:{child.Key}", data);
  }
  ```

- [ ] **Step 4: 运行集成测试，确认 GREEN**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo --filter "FullyQualifiedName~GirvsSerilogSinkIntegrationTests"`
  Expected: PASS — 3 项集成测试通过。

- [ ] **Step 5: 运行全部核心测试，确认无回归**

  Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo`
  Expected: PASS — 全部 5+3=8 项测试通过。

- [ ] **Step 6: 构建 Girvs 核心项目确认编译**

  Run: `dotnet build Girvs/Girvs.csproj --nologo`
  Expected: 退出码 0，零错误。

---

### Task 4: 迁移 Sample.Gateway 使用新 API

**Files:**
- Modify: `samples/Sample.Gateway/Startup.cs`
- Delete: 旧 `tests/Sample.Gateway.Tests/StartupElasticsearchLoggingTests.cs` 中的反射测试（替换为新测试）

**Interfaces:**
- Consumes: `SerilogSinkServiceCollectionExtensions.AddSerilogSink`。
- Produces: 新 `Sample.Gateway.Startup` 使用 `AddSerilogSink` 而非反射。

- [ ] **Step 1: 改写 Startup.cs**

  `samples/Sample.Gateway/Startup.cs` — 移除 `AddElasticsearchLogging` 方法及 `Configure` 中的调用，改为在 `ConfigureServices` 中注册：

  ```csharp
  using Girvs;
  using Girvs.Gateway;
  using Girvs.ServiceGovernance.Discovery;
  using Microsoft.OpenApi;
  using Serilog;
  using Serilog.Sinks.Elasticsearch;

  namespace Sample.Gateway;

  public class Startup : IGirvsStartup
  {
      private readonly IConfiguration _configuration;

      public Startup(IConfiguration configuration, IWebHostEnvironment env)
      {
          _configuration = configuration;
      }

      public void ConfigureServices(IServiceCollection services)
      {
          services.AddGirvsGateway(_configuration);
          services.AddSingleton<SwaggerEndpointEnumerator>();
          services.AddEndpointsApiExplorer();
          services.AddSwaggerGen(c =>
          {
              c.SwaggerDoc("v1",
                  new OpenApiInfo { Title = AppDomain.CurrentDomain.FriendlyName, Version = "v1" });
          });

          // 代码方式注册 Elasticsearch sink
          var esNodeUrl = _configuration["Resources:elastic-search:Settings:NodeUrls"];
          if (!string.IsNullOrEmpty(esNodeUrl)
              && Uri.TryCreate(esNodeUrl, UriKind.Absolute, out var nodeUri))
          {
              var deployName = Environment.GetEnvironmentVariable("DEPLOY_SYSTEM_NAME") ?? "girvs";
              var serverName = Environment.GetEnvironmentVariable("CURRENT_SERVER_NAME") ?? "gateway";
              var indexFormat = $"{deployName}-{serverName}-webapi-{{0:yyyy.MM.dd}}".ToLowerInvariant();

              services.AddSerilogSink(
                  config => config.WriteTo.Elasticsearch(
                      new ElasticsearchSinkOptions(nodeUri)
                      {
                          IndexFormat = indexFormat,
                          AutoRegisterTemplate = true,
                          EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                      }),
                  "Elasticsearch");
          }
      }

      // Configure 方法保持不变（移除 AddElasticsearchLogging 调用）
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
          var serviceDirectory = app.ApplicationServices.GetRequiredService<IServiceDirectory>();
          var swaggerEndpoints =
              app.ApplicationServices.GetRequiredService<SwaggerEndpointEnumerator>();

          swaggerEndpoints.Refresh(serviceDirectory);

          app.UseMiddleware<SwaggerFilterMiddleware>();
          app.UseSwagger();
          app.UseSwaggerUI(c =>
          {
              c.RoutePrefix = "girvs_swagger";
              c.DocumentTitle = "Girvs Sample Gateway API Docs";
              c.ConfigObject.Urls = swaggerEndpoints;
          });

          if (app is not IEndpointRouteBuilder endpoints)
              return;

          endpoints.MapGet("/", async context =>
          {
              await context.Response.WriteAsync("Welcome to ScsApiGateway!");
          });

          if (env.IsDevelopment())
              endpoints.MapSwagger("{documentName}/api-docs");

          endpoints.MapReverseProxy();
          endpoints.MapGet("/health", () => Results.Ok("ok"));
      }
  }
  ```

- [ ] **Step 2: 更新 Sample.Gateway 测试**

  替换 `tests/Sample.Gateway.Tests/StartupElasticsearchLoggingTests.cs` 为新的 ES 注册测试：

  ```csharp
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Sample.Gateway;

  namespace Sample.Gateway.Tests;

  public class StartupElasticsearchLoggingTests
  {
      public StartupElasticsearchLoggingTests()
      {
          GirvsSerilogSinkRegistry.Registrations.Clear();
      }

      [Fact]
      public void ConfigureServices_ES地址配置存在_向注册表添加Elasticsearch覆盖()
      {
          var configuration = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Resources:elastic-search:Settings:NodeUrls"] = "http://localhost:9200",
              })
              .Build();

          var services = new ServiceCollection();
          var startup = new Startup(configuration, null!);
          startup.ConfigureServices(services);

          var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();
          Assert.Single(snapshot);
          Assert.Contains("Elasticsearch", snapshot[0].OverriddenSinkTypeNames,
              StringComparer.OrdinalIgnoreCase);
      }

      [Fact]
      public void ConfigureServices_ES地址未配置_不向注册表添加任何条目()
      {
          var configuration = new ConfigurationBuilder()
              .AddInMemoryCollection([])
              .Build();

          var services = new ServiceCollection();
          var startup = new Startup(configuration, null!);
          startup.ConfigureServices(services);

          var snapshot = GirvsSerilogSinkRegistry.SnapshotAndClear();
          Assert.Empty(snapshot);
      }
  }
  ```

- [ ] **Step 3: 运行 Sample.Gateway 测试**

  Run: `dotnet test tests/Sample.Gateway.Tests/Sample.Gateway.Tests.csproj --nologo`
  Expected: PASS — 两项测试通过。

- [ ] **Step 4: 构建 Sample.Gateway**

  Run: `dotnet build samples/Sample.Gateway/Sample.Gateway.csproj --nologo`
  Expected: 退出码 0，零警告零错误。

---

### Task 5: 全量构建与测试验证

**Files:**
- 无新建或修改。
- 产出：全量构建与测试执行证据。

**Interfaces:**
- Consumes: Task 1-4 的全部产出。
- Produces: 验收证据。

- [ ] **Step 1: 执行全量构建**

  Run: `dotnet build Girvs.slnx --nologo`
  Expected: 退出码 0，所有项目构建通过，Girvs 核心零错误。

- [ ] **Step 2: 执行全量测试**

  Run: `dotnet test Girvs.slnx --nologo`
  Expected: 退出码 0。若存在环境相关基线失败，记录完整失败列表并确认本次改造未引入新失败。

- [ ] **Step 3: 核对 Spec 验收标准**

  逐项核对 `docs/superpowers/specs/2026-08-03-serilog-code-sink-registration-design.md` 的七项验收标准：

  1. `AddSerilogSink` File sink 注册 → 由 Task 3 集成测试中代码委托被调用 + `SetEquals` 替换测试覆盖。
  2. 代码覆盖 File、配置 File 跳过 → 由 Task 3 中 TestCorrelator 覆盖/未覆盖两项测试覆盖。
  3. 后者替换前者 → 由 Task 2 中 `AddSerilogSink_相同覆盖集合注册两次_后者替换前者` 测试覆盖。
  4. 委托抛异常不崩溃 → 由 Task 3 中 `HostUseSerilog_代码委托抛异常_App不崩溃且Console兜底有效` 测试覆盖。
  5. 无代码/配置 Console 兜底 → 由既有 `HostUseSerilog_无业务Serilog配置_Information日志写入Console` 测试覆盖。
  6. 全量构建与测试 → 由 Step 1 和 Step 2 覆盖。
  7. Girvs 不引用具体 Sink 包 → 检查 `Girvs.csproj` 的包引用无新增 File/ES/OTLP sink 包。
