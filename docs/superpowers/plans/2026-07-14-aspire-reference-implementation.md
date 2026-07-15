# Girvs + Aspire 参照实现与分步验证 实施计划（计划 1）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Girvs 仓库内建一个最小但完整、能端到端跑通的参照系统（AppHost + 2 个业务服务 + 1 个 Worker），把 `Girvs.Aspire`/`Girvs.Aspire.Hosting` 第一次真正"活"一遍，并在这个真实系统里落地并验证两项框架增强（共享配置注入、生产资源外部引用），产出一份可供真实业务照抄的模板。

**Architecture:** 参照系统放在独立解决方案 `samples/GirvsAspireSample.slnx`（不进 `Girvs.slnx`，避免"构建即打包"波及），所有样例项目 `IsPackable=false`、`GeneratePackageOnBuild=false`，`ProjectReference` 直接引用框架源码工程（而非 NuGet 包），改框架代码立即生效。每步先让系统真跑通（`dotnet run` AppHost + Dashboard 观察 / 集成测试）再进下一步；框架能力的改动仍配单元测试进既有 `tests/Girvs.Aspire.Hosting.Tests`。

**Tech Stack:** .NET 10（本机 SDK 10.0.109）、Aspire.Hosting 13.4.6、Aspire.AppHost.Sdk、YARP（网关，本计划仅静态路由占位，K8s 动态发现属计划 2）、xUnit 2.9.3。`aspire` CLI 本机不可用——AppHost 一律用 `dotnet run` 启动、样例项目用手写 csproj 或 `dotnet new` 创建，不依赖 `aspire` 命令。

## Global Constraints

- 参照系统全部项目：`<TargetFramework>net10.0</TargetFramework>`、`<IsPackable>false</IsPackable>`、`<GeneratePackageOnBuild>false</GeneratePackageOnBuild>`。
- 参照系统所在解决方案 `samples/GirvsAspireSample.slnx` 独立于 `Girvs.slnx`；不得把样例项目加进 `Girvs.slnx`。
- 样例项目对框架的引用一律用 `ProjectReference` 指向 `..\..\Girvs.Aspire\Girvs.Aspire.csproj` 等源码工程，不用 `<PackageReference Include="Girvs.*">`。
- 消费侧启动契约（已核对）：`IGirvsStartup` 定义 `void ConfigureServices(IServiceCollection services)` 与 `void Configure(IApplicationBuilder application, IWebHostEnvironment env)`；宿主经 `GirvsHostBuilderManager.CreateBuilder(args)` 得到 `WebApplicationBuilder`、再 `GirvsHostBuilderManager.CreateGrivsHostBuilder<Startup>(builder)` 接入 Girvs 模块机制。`AspireModule`（`IAppModuleStartup`, Order=-10000）经模块自发现随包引用自动生效，无需显式注册。
- **每个样例服务的实际 Program.cs / Startup 写法，须比对一个已知可运行的 Girvs 服务的现行写法**（若手头无参照，实现第一步时先写最小版本并以"能 `dotnet run` 起来且 `/health` 返回 200"为通过判据来校正，不照搬本计划伪代码的每一行）。本计划给出的服务端启动代码是结构示意，以真实跑通为准。
- 版本号：本计划不改 `Directory.Build.props` 版本号（不发包，纯验证 + 样例）。涉及框架代码修改的 Task（共享配置、外部资源引用）完成后，版本号提升留待发布计划统一处理。
- Aspire publish 专属 API（`ExecutionContext.IsPublishMode`、`AddConnectionString`/`AddParameter`、publish-mode 测试 builder、`GetEnvironmentVariableValuesAsync`）以本机 Aspire 13.4.6 实际签名为准；本计划所示为预期写法，签名有出入时保持行为语义不变。

---

### Task 1: 样例解决方案骨架 + 两个空壳服务能被 AppHost 跑通

**目标交付物**：`dotnet run` 样例 AppHost，Aspire Dashboard 显示两个服务处于 Running、各自 `/health` 返回 200。这是"框架骨架能跑"的第一个证明。

**Files:**
- Create: `samples/GirvsAspireSample.slnx`
- Create: `samples/Sample.ServiceA/Sample.ServiceA.csproj`
- Create: `samples/Sample.ServiceA/Program.cs`
- Create: `samples/Sample.ServiceA/Startup.cs`
- Create: `samples/Sample.ServiceA/appsettings.json`
- Create: `samples/Sample.ServiceB/*`（结构同 ServiceA）
- Create: `samples/Sample.AppHost/Sample.AppHost.csproj`
- Create: `samples/Sample.AppHost/Program.cs`
- Create: `samples/Sample.Modules/Sample.Modules.csproj`（放根模块声明类，供 AppHost `typeof(...)` 引用）
- Create: `samples/Sample.Modules/ServiceAModule.cs`、`samples/Sample.Modules/ServiceBModule.cs`

**Interfaces:**
- Produces:
  - `Sample.Modules` 程序集含 `public class ServiceAModule;`、`public class ServiceBModule;`（本 Task 无 `[DependsOn]`，Task 3 起再加组件声明）。
  - AppHost `Program.cs` 用 `builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule))` 编排。

- [ ] **Step 1: 创建业务服务项目（ServiceA）**

`samples/Sample.ServiceA/Sample.ServiceA.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Girvs\Girvs.csproj" />
    <ProjectReference Include="..\..\Girvs.Aspire\Girvs.Aspire.csproj" />
  </ItemGroup>
</Project>
```

`samples/Sample.ServiceA/Startup.cs`（**已按实测真实 API 定稿**——命名空间为 `Girvs`，非 `Girvs.Startup`）：

```csharp
using Girvs;

namespace Sample.ServiceA;

public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 请求管道与端点映射由 CreateGirvsWebApplicationBuilder 统一处理
        // （内部调用 ConfigureRequestPipeline + ConfigureEndpointRouteBuilder，
        //  AspireModule 的 /health、/alive 端点在此自动映射），本壳服务无需额外配置
    }
}
```

`samples/Sample.ServiceA/Program.cs`（**已按实测真实 API 定稿**）：

```csharp
using Girvs;

var app = GirvsHostBuilderManager.CreateGirvsWebApplicationBuilder(args);
app.Run();
```

> **实测确认的启动契约**（`Girvs/GirvsHostBuilderManager.cs`、`Girvs/IGirvsStartup.cs`，命名空间均为 `Girvs`）：`CreateGirvsWebApplicationBuilder(string[] args)` → `WebApplication`，它自动发现所有 `IGirvsStartup` 实现、依次 `ConfigureServices`、跑模块系统（`ConfigureApplicationServices`）、`Build`、依次 `Configure`、再 `ConfigureRequestPipeline` + `ConfigureEndpointRouteBuilder`。因此 Program.cs 只需两行，Startup.Configure 留空即可。（会话前期"读到"的 `CreateBuilder(args)` + `CreateGrivsHostBuilder<Startup>(builder).UseGirvs()` 等签名为环境读取故障产生的错误信息，实际不存在——已废弃。）

`samples/Sample.ServiceA/appsettings.json`：

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

- [ ] **Step 2: 复制出 ServiceB**

同结构创建 `samples/Sample.ServiceB/`（命名空间 `Sample.ServiceB`，csproj/Program/Startup/appsettings 对应改名）。

- [ ] **Step 3: 创建根模块声明程序集**

`samples/Sample.Modules/Sample.Modules.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Girvs\Girvs.csproj" />
  </ItemGroup>
</Project>
```

`samples/Sample.Modules/ServiceAModule.cs`：

```csharp
namespace Sample.Modules;

// 本 Task 暂无组件依赖；Task 3 起补 [DependsOn(...)]
public class ServiceAModule;
```

`samples/Sample.Modules/ServiceBModule.cs`：同上，类名 `ServiceBModule`。

- [ ] **Step 4: 创建 AppHost 项目**

`samples/Sample.AppHost/Sample.AppHost.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.4.6" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsAspireHost>true</IsAspireHost>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Girvs.Aspire.Hosting\Girvs.Aspire.Hosting.csproj" />
    <ProjectReference Include="..\Sample.Modules\Sample.Modules.csproj" />
    <ProjectReference Include="..\Sample.ServiceA\Sample.ServiceA.csproj" IsAspireProjectResource="true" />
    <ProjectReference Include="..\Sample.ServiceB\Sample.ServiceB.csproj" IsAspireProjectResource="true" />
  </ItemGroup>
</Project>
```

> `Aspire.AppHost.Sdk` 版本须与 `Aspire.Hosting` 13.4.6 对齐。`IsAspireProjectResource="true"` 让服务作为编排目标（生成 `Projects.Sample_ServiceA` 元数据类）；`Sample.Modules` 用普通引用以便 `typeof(ServiceAModule)` 可编译。

`samples/Sample.AppHost/Program.cs`：

```csharp
using Girvs.Aspire.Hosting;
using Sample.Modules;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule));
builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b", typeof(ServiceBModule));

builder.Build().Run();
```

- [ ] **Step 5: 创建独立解决方案并加入全部样例项目**

`samples/GirvsAspireSample.slnx`：

```xml
<Solution>
  <Project Path="Sample.AppHost/Sample.AppHost.csproj" />
  <Project Path="Sample.Modules/Sample.Modules.csproj" />
  <Project Path="Sample.ServiceA/Sample.ServiceA.csproj" />
  <Project Path="Sample.ServiceB/Sample.ServiceB.csproj" />
</Solution>
```

- [ ] **Step 6: 构建样例解决方案**

Run: `dotnet build samples/GirvsAspireSample.slnx`
Expected: 构建成功。若因消费侧启动 API（`Build()`/`UseGirvs()`/`Run()`）与示意代码不符而失败，按框架真实 API 修正 `Program.cs`（这是 Global Constraints 已声明的校正点），直到构建通过。

- [ ] **Step 7: 端到端跑通验证（本 Task 的真正交付判据）**

Run: `dotnet run --project samples/Sample.AppHost`（后台启动），记录控制台输出的 Aspire Dashboard URL。
验证（人工观察，或用 curl）：
1. Dashboard 中 `service-a`、`service-b` 状态为 Running；
2. 两个服务的 `/health` 与 `/alive` 端点返回 200（`AspireModule.ConfigureMapEndpointRoute` 已映射）；
3. Dashboard 的 Console/Structured logs 能看到两个服务的启动日志。

若跑不通，这就是"框架骨架自洽性"的第一个真实缺陷点——定位并修复（可能涉及框架侧 Program 启动流程），修复后再继续。**不要在骨架未跑通时进入 Task 2。**

> **✅ Task 1 实测结论（已完成并跑通）**：AppHost 编排下 `service-a`(端口 45571)、`service-b`(端口 45325) 均 Running，各自 `/health`、`/alive` 返回 200，Dashboard 正常，OTLP 端点已注入（OTel 生效）。执行中发现并固化了三个原计划未预见的修正点，**均为样例/工程层面、非框架代码改动**，对后续 Task 与业务迁移同样适用：
>
> 1. **目标框架收敛**：样例项目继承仓库根 `Directory.Build.props` 的 `net8;net9;net10` 多目标，会拉着只支持 net10 的 `Girvs.Aspire` 一起构建 net8/9 而失败。修法：新增 `samples/Directory.Build.props`，`Import` 根 props 后把 `<TargetFrameworks>` 覆盖为 `net10.0`、`GeneratePackageOnBuild=false`、`IsPackable=false`。一处生效，覆盖当前与后续全部样例项目。
> 2. **AppHost 编译引用**：Aspire.AppHost.Sdk 默认把所有 `ProjectReference` 当编排资源（`ReferenceOutputAssembly=false`），导致 `Girvs.Aspire.Hosting`（`AddGirvsProject` 扩展）与 `Sample.Modules`（`typeof(XxxModule)`）的类型对 AppHost 不可见。修法：这两个引用显式加 `IsAspireProjectResource="false"`（普通编译引用）；被编排的 ServiceA/B 保持 `true`。
> 3. **服务端点（关键，对迁移重要）**：Aspire 的 `AddProject` 依赖各服务 `Properties/launchSettings.json` 的 `applicationUrl` 分配端点。样例服务最初无 launchSettings，Aspire 未注入任何端口变量（实测服务进程 environ 中只有 `OTEL_*`，无 `ASPNETCORE_URLS`/`Kestrel__*`），两个服务都回退默认 5000 端口冲突、只有一个能起。修法：每个服务加 `Properties/launchSettings.json` 指定各自 `applicationUrl`（ServiceA `http://localhost:5101`、ServiceB `http://localhost:5102`）。**迁移提示**：真实 Girvs 服务接入 Aspire 编排必须具备 launchSettings 或在 AppHost 显式配置端点——需写入 `docs/aspire/升级方案.md` 的迁移清单。

- [x] **Step 8: 提交**

```bash
git add samples/
git commit -m "test: 新增 Girvs+Aspire 最小参照系统骨架并端到端跑通两个服务"
```

---

### Task 2: 接入真实组件（Cache/EventBus/EFCore），验证连接串自动注入

**目标交付物**：ServiceA 声明使用 Cache + EFCore、ServiceB 声明使用 EventBus，`dotnet run` AppHost 时 Aspire 自动拉起 Redis/MySQL/RabbitMQ 容器，服务真实连上（读写一次），证明 `AddGirvsProject` 的资源接线 + 各组件 `ApplyAspireConnectionString` 在运行时真的生效。

> **实施说明（执行中确定）**：本 Task 按组件**逐个增量验证**（Cache → EventBus → EFCore），每个组件独立跑通再叠加下一个——因为发现三者有不同的真实依赖：EventBus 的 CAP 需要一个存储库（`DbType=MySql` + `DbConnectionString`）而 `EventBusResourceContributor` 只建 RabbitMQ 不建 CAP 库；EFCore 要真正验证 DB 往返需定义最小 `GirvsDbContext` + 实体。分开验证比一次性全绑更好定位问题。
>
> **前置环境**：Aspire 在 Run 模式下拉起容器需 Docker 能拉取镜像。本机初次遇到 docker 守护进程未走代理导致 `redis` 镜像拉取 `connection refused`，由用户配置代理后解决——**迁移提示**：接入 Aspire 的开发机必须保证 Docker 能拉取 `redis`/`mysql`/`rabbitmq` 镜像（国内需配置镜像加速或代理）。
>
> **✅ Cache 增量已完成并跑通**：Aspire 自动拉起 `girvs-cache`(redis) 容器，注入的**带密码连接串**在运行时覆盖 appsettings 默认值并生效（`docker exec redis-cli` 裸连报 `NOAUTH` 反证服务用的是注入串），`GET /selfcheck/cache` 返回 `{"match":true}`。发现一处**对迁移重要**的框架行为：新 `CreateGirvsWebApplicationBuilder` 启动模型下，框架的 `ConfigureEndpointRouteBuilder` 只映射各模块端点（如 AspireModule 的 `/health`），**普通 MVC 控制器需在 `Startup.Configure` 里显式映射**——传入的 `app` 即 `WebApplication`（实现 `IEndpointRouteBuilder`），`if (app is IEndpointRouteBuilder e) e.MapControllers();` 即可。这与 Girvs 真实服务 Startup.Configure 调 `MapControllers` 一致，需写入迁移清单。

**Files:**
- Modify: `samples/Sample.Modules/ServiceAModule.cs`、`ServiceBModule.cs`（加 `[DependsOn]`）
- Modify: `samples/Sample.ServiceA/Sample.ServiceA.csproj`、`ServiceB` csproj（加组件包 `ProjectReference`）
- Modify: `samples/Sample.ServiceA/appsettings.json`、`ServiceB`（加 `ModuleConfigurations` 节声明资源形态）
- Modify: `samples/Sample.ServiceA/Startup.cs`（加一个读写缓存/DB 的自检端点）、`ServiceB`（加一个发/收事件的自检）

**Interfaces:**
- Consumes: `AddGirvsProject`（Task 1）、`CacheResourceContributor`/`EventBusResourceContributor`/`DatabaseResourceContributor`（现有框架）。
- Produces: ServiceA 一个 `GET /selfcheck/cache` 端点（写一个 key 再读回，返回是否一致）；ServiceB 一个 `GET /selfcheck/eventbus`（发一条 CAP 消息，返回是否发布成功）。

- [ ] **Step 1: ServiceA 声明 Cache + EFCore 依赖**

`samples/Sample.Modules/ServiceAModule.cs`：

```csharp
using Girvs.Cache;
using Girvs.EntityFrameworkCore;

namespace Sample.Modules;

[DependsOn(typeof(GirvsCacheModule), typeof(GirvsEntityFrameworkCoreModule))]
public class ServiceAModule;
```

`Sample.ServiceA.csproj` 增加：

```xml
    <ProjectReference Include="..\..\Girvs.Cache\Girvs.Cache.csproj" />
    <ProjectReference Include="..\..\Girvs.EntityFrameworkCore\Girvs.EntityFrameworkCore.csproj" />
```

`Sample.Modules.csproj` 增加对 `Girvs.Cache`、`Girvs.EntityFrameworkCore` 的 `ProjectReference`（`[DependsOn]` 需要引用这些类型）。

`Sample.ServiceA/appsettings.json` 增加 `ModuleConfigurations`（资源形态真源，供 AppHost 读取决定建 Redis + MySQL）：

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "ModuleConfigurations": {
    "CacheConfig": { "DistributedCacheConfig": { "Enabled": true, "DistributedCacheType": "Redis" } },
    "DbConfig": {
      "DataConnectionConfigs": [ { "Name": "default", "UseDataType": "MySql" } ]
    }
  }
}
```

> `CacheConfig`/`DbConfig` 的确切 JSON 结构须比对各组件 `Configuration/*.cs` 的属性名（如 `DistributedCacheConfig.Enabled`、`DataConnectionConfig.Name`/`UseDataType`），以组件真实反序列化通过为准。

- [ ] **Step 2: ServiceB 声明 EventBus 依赖**

`ServiceBModule.cs`：`[DependsOn(typeof(EventBusModule))]`；`Sample.ServiceB.csproj` 与 `Sample.Modules.csproj` 加 `Girvs.EventBus` 引用；`ServiceB/appsettings.json` 的 `ModuleConfigurations.EventBusConfig.EventBusType` 设为 `"RabbitMQ"`（对应组件配置结构核对）。

- [ ] **Step 3: 加自检端点**

在 ServiceA `Startup`/控制器加 `GET /selfcheck/cache`：注入 `IStaticCacheManager`（或框架缓存接口），写入 `selfcheck=ok` 再读回，返回 `{ "match": true }`。在 ServiceB 加 `GET /selfcheck/eventbus`：注入 CAP `ICapPublisher`，发一条测试消息，返回 `{ "published": true }`。

> 具体缓存/发布接口名以组件真实 API 为准（缓存看 `Girvs.Cache` 的 `IStaticCacheManager`，事件看 `Girvs.EventBus` 的 CAP 封装）。

- [ ] **Step 4: 端到端验证**

Run: `dotnet run --project samples/Sample.AppHost`
验证：
1. Dashboard 出现 `girvs-cache`(Redis)、`girvs-mysql`、`girvs-eventbus-rabbitmq` 资源且 Running；
2. `service-a` 的 `girvs-db-service-a-default` 数据库资源已创建；
3. `curl service-a/selfcheck/cache` 返回 `match: true`（证明 Redis 连接串被自动注入且缓存真的可用）；
4. `curl service-b/selfcheck/eventbus` 返回 `published: true`（证明 RabbitMQ 连接串自动注入且 CAP 可用）。

这一步会暴露 `ApplyAspireConnectionString` 在真实运行时（而非单测的 in-memory config）下的任何格式/时序问题——这正是要在参照实现里抓出来的东西。

- [ ] **Step 5: 提交**

```bash
git add samples/
git commit -m "test: 参照系统接入 Cache/EventBus/EFCore 并验证连接串自动注入运行时生效"
```

---

### Task 3: 服务间调用 + Aspire 服务发现跑通

**目标交付物**：ServiceA 通过服务发现（而非硬编码地址）调用 ServiceB 的一个端点，链路在 Dashboard 的分布式追踪里可见。证明 `AspireModule` 注册的 `Microsoft.Extensions.ServiceDiscovery` + HttpClient 弹性在真实拓扑下工作。

**Files:**
- Modify: `samples/Sample.AppHost/Program.cs`（让 service-a `WithReference` service-b 以注入其发现地址）
- Modify: `samples/Sample.ServiceB/*`（加一个被调用的 `GET /ping` 返回 `pong`）
- Modify: `samples/Sample.ServiceA/*`（加 `GET /callb` 用命名 HttpClient 以 `https://service-b` 调 ServiceB 的 `/ping`）

**Interfaces:**
- Consumes: 服务发现（`AspireModule` 已 `AddServiceDiscovery` + `ConfigureHttpClientDefaults`）。
- Produces: ServiceA `GET /callb` 返回从 ServiceB 取回的 `pong`。

- [ ] **Step 1: AppHost 建立 service-a → service-b 引用**

修改 `Sample.AppHost/Program.cs`：

```csharp
var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b", typeof(ServiceBModule));
builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule))
    .WithReference(serviceB);
```

> `AddGirvsProject` 当前返回 `IResourceBuilder<ProjectResource>`，可直接链式 `.WithReference(serviceB)` 注入 service-b 的发现地址（环境变量 `services__service-b__...`）。

- [ ] **Step 2: ServiceB 加 /ping，ServiceA 加 /callb**

ServiceB：`GET /ping` → `"pong"`。
ServiceA：注册一个 `HttpClient`，`GET /callb` 内 `httpClient.GetStringAsync("https://service-b/ping")`，返回结果。`https://service-b` 的主机名由服务发现解析（`AspireModule` 已为默认 HttpClient 启用 `AddServiceDiscovery`）。

- [ ] **Step 3: 端到端验证**

Run: `dotnet run --project samples/Sample.AppHost`，`curl service-a/callb`
Expected: 返回 `pong`；Dashboard 的 Traces 里能看到一条 `service-a → service-b` 的跨服务调用链（证明 OTel + 服务发现 + HttpClient 全链路通）。

- [ ] **Step 4: 提交**

```bash
git add samples/
git commit -m "test: 参照系统验证 Aspire 服务发现与跨服务调用链路"
```

---

### Task 4: 框架增强——共享配置注入（含单测）+ 在参照系统验证

**目标交付物**：`Girvs.Aspire.Hosting` 支持 `AddGirvsSharedConfiguration` 一次声明、注入所有服务；参照系统里两个服务共享同一份日志等级 + JwtSecret，改一处两个服务都变。含单元测试。

**Files:**
- Create: `Girvs.Aspire.Hosting/GirvsSharedConfiguration.cs`
- Create: `Girvs.Aspire.Hosting/GirvsSharedConfigurationExtensions.cs`
- Modify: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigurationTests.cs`
- Modify: `samples/Sample.AppHost/Program.cs`
- Modify: 两个服务加一个 `GET /selfcheck/config` 回显生效的日志等级

**Interfaces:**
- Produces:
  - `class GirvsSharedConfiguration`：`void AddSetting(string key, string value)`、`void AddSecret(string key, IResourceBuilder<ParameterResource> parameter)`、`IReadOnlyList<(string Key, string Value)> Settings`、`IReadOnlyList<(string Key, IResourceBuilder<ParameterResource> Parameter)> Secrets`。
  - `static IDistributedApplicationBuilder AddGirvsSharedConfiguration(this IDistributedApplicationBuilder, Action<GirvsSharedConfiguration>)`、`static GirvsSharedConfiguration GetShared(IDistributedApplicationBuilder)`。
  - `WireGirvsResources` 末尾对每个服务注入共享配置（`:` → `__` 环境变量键名转换）。

- [ ] **Step 1: 写失败单测（分类 + 注入）**

Create `tests/Girvs.Aspire.Hosting.Tests/GirvsSharedConfigurationTests.cs`：

```csharp
namespace Girvs.Aspire.Hosting.Tests;

public class GirvsSharedConfigurationTests
{
    private class NoResourceModule;

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true });

    [Fact]
    public void 声明共享配置_非敏感项进Settings敏感项进Secrets()
    {
        var builder = CreateBuilder();
        var jwt = builder.AddParameter("jwt-secret", secret: true);

        builder.AddGirvsSharedConfiguration(shared =>
        {
            shared.AddSetting("Logging:LogLevel:Default", "Information");
            shared.AddSecret("Jwt:Secret", jwt);
        });

        var config = GirvsSharedConfigurationExtensions.GetShared(builder);
        Assert.Contains(config.Settings, s => s.Key == "Logging:LogLevel:Default" && s.Value == "Information");
        Assert.Contains(config.Secrets, s => s.Key == "Jwt:Secret");
        Assert.DoesNotContain(config.Settings, s => s.Key == "Jwt:Secret");
    }

    [Fact]
    public async Task 服务接线后_共享非敏感配置作为环境变量注入()
    {
        var builder = CreateBuilder();
        builder.AddGirvsSharedConfiguration(shared =>
            shared.AddSetting("Logging:LogLevel:Default", "Warning"));

        var directory = Directory.CreateTempSubdirectory("girvs-shared").FullName;
        File.WriteAllText(Path.Combine(directory, "svc.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>""");
        var project = builder.AddProject("svc", Path.Combine(directory, "svc.csproj"));

        builder.WireGirvsResources(project, directory, typeof(NoResourceModule));

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish);
        Assert.Equal("Warning", env["Logging__LogLevel__Default"]);

        Directory.Delete(directory, true);
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsSharedConfigurationTests"`
Expected: 编译失败（类型不存在）。

- [ ] **Step 3: 实现数据模型**

Create `Girvs.Aspire.Hosting/GirvsSharedConfiguration.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

/// <summary>跨服务共享配置声明：非敏感项与敏感项分开收集，供 AddGirvsProject 注入各服务。</summary>
public class GirvsSharedConfiguration
{
    private readonly List<(string Key, string Value)> _settings = new();
    private readonly List<(string Key, IResourceBuilder<ParameterResource> Parameter)> _secrets = new();

    public void AddSetting(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _settings.Add((key, value));
    }

    public void AddSecret(string key, IResourceBuilder<ParameterResource> parameter)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(parameter);
        _secrets.Add((key, parameter));
    }

    public IReadOnlyList<(string Key, string Value)> Settings => _settings;
    public IReadOnlyList<(string Key, IResourceBuilder<ParameterResource> Parameter)> Secrets => _secrets;
}
```

- [ ] **Step 4: 实现扩展与存取**

Create `Girvs.Aspire.Hosting/GirvsSharedConfigurationExtensions.cs`：

```csharp
using System.Runtime.CompilerServices;

namespace Girvs.Aspire.Hosting;

public static class GirvsSharedConfigurationExtensions
{
    private static readonly ConditionalWeakTable<IDistributedApplicationBuilder, GirvsSharedConfiguration> Store = new();

    public static IDistributedApplicationBuilder AddGirvsSharedConfiguration(
        this IDistributedApplicationBuilder builder,
        Action<GirvsSharedConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var shared = new GirvsSharedConfiguration();
        configure(shared);
        Store.AddOrUpdate(builder, shared);
        return builder;
    }

    public static GirvsSharedConfiguration GetShared(IDistributedApplicationBuilder builder) =>
        Store.TryGetValue(builder, out var shared) ? shared : new GirvsSharedConfiguration();
}
```

- [ ] **Step 5: 在 WireGirvsResources 末尾接线共享配置**

修改 `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`，在 `foreach` 循环后、`return project;` 前插入调用，并新增私有方法：

```csharp
        ApplySharedConfiguration(builder, project);

        return project;
    }

    private static void ApplySharedConfiguration(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> project)
    {
        var shared = GirvsSharedConfigurationExtensions.GetShared(builder);
        foreach (var (key, value) in shared.Settings)
            project.WithEnvironment(ToEnvKey(key), value);
        foreach (var (key, parameter) in shared.Secrets)
            project.WithEnvironment(ToEnvKey(key), parameter);
    }

    private static string ToEnvKey(string configKey) => configKey.Replace(":", "__");
```

- [ ] **Step 6: 运行单测确认通过并回归**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 全绿（新共享配置测试 + 既有编排测试）。

- [ ] **Step 7: 在参照系统验证共享配置真实生效**

修改 `Sample.AppHost/Program.cs` 顶部：

```csharp
var jwt = builder.AddParameter("jwt-secret", "sample-dev-secret");
builder.AddGirvsSharedConfiguration(shared =>
{
    shared.AddSetting("Logging:LogLevel:Default", "Warning");
    shared.AddSecret("Jwt:Secret", jwt);
});
```

两个服务各加 `GET /selfcheck/config`，注入 `IConfiguration` 回显 `Logging:LogLevel:Default` 与 `Jwt:Secret` 是否有值。

Run: `dotnet run --project samples/Sample.AppHost`，分别 `curl service-a/selfcheck/config`、`curl service-b/selfcheck/config`
Expected: 两个服务都回显 `Logging:LogLevel:Default = Warning`（覆盖了各自 appsettings.json 里的 Information）、`Jwt:Secret` 有值。改 AppHost 里的 `Warning` 为 `Error` 重跑，两个服务同步变化——证明"一处声明、全体生效、覆盖本地"。

- [ ] **Step 8: 提交**

```bash
git add Girvs.Aspire.Hosting/ tests/Girvs.Aspire.Hosting.Tests/ samples/
git commit -m "feat: 共享配置注入能力 + 参照系统验证一处声明全体生效"
```

---

### Task 5: 框架增强——生产资源外部引用（含单测）+ aspire publish 清单验证

**目标交付物**：Publish 模式下 Cache/EventBus/EFCore 改为引用外部连接串参数（不建容器）；对参照系统执行 publish 生成清单，人工审清单：业务服务是 Deployment/Service、基础设施是外部连接串引用而非容器。含单元测试。（真集群部署不在本计划——只验证到清单正确。）

**Files:**
- Modify: `Girvs.Aspire.Hosting/Contributors/CacheResourceContributor.cs`
- Modify: `Girvs.Aspire.Hosting/Contributors/EventBusResourceContributor.cs`
- Modify: `Girvs.Aspire.Hosting/Contributors/DatabaseResourceContributor.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectOrchestrationTests.cs`（加 `CreatePublishBuilder` + publish 分支测试）

**Interfaces:**
- Consumes: `GirvsOrchestrationContext`、`context.Builder.ExecutionContext.IsPublishMode`。
- Produces: publish 模式下 `girvs-cache`/`girvs-eventbus-*`/`girvs-db-<service>-<name>` 均为 `IResourceWithConnectionString`（外部引用），run 模式保持容器语义不变。外部参数命名：`girvs-cache-connection-string`、`girvs-eventbus-rabbitmq-connection-string`、`girvs-eventbus-redis-connection-string`、`girvs-db-<service>-<name>-connection-string`。

- [ ] **Step 1: 加 publish-mode 测试 builder + 首个失败测试**

在 `GirvsProjectOrchestrationTests` 加：

```csharp
private static IDistributedApplicationBuilder CreatePublishBuilder() =>
    DistributedApplication.CreateBuilder(
        new DistributedApplicationOptions
        {
            DisableDashboard = true,
            Args = ["--operation", "publish", "--publisher", "manifest", "--output-path", "."]
        });

[Fact]
public void 发布模式Builder为IsPublishMode()
{
    Assert.True(CreatePublishBuilder().ExecutionContext.IsPublishMode);
}

[Fact]
public void 发布模式声明Cache模块_创建外部连接串引用而非Redis容器()
{
    var builder = CreatePublishBuilder();
    var directory = CreateServiceProjectDirectory("order-api");
    var project = AddServiceProject(builder, "order-api", directory);

    builder.WireGirvsResources(project, directory, typeof(CacheOnlyModule));

    Assert.Empty(builder.Resources.OfType<RedisResource>());
    Assert.Contains(builder.Resources.OfType<IResourceWithConnectionString>(),
        r => r.Name == "girvs-cache");
}
```

- [ ] **Step 2: 运行确认 `发布模式Builder为IsPublishMode` 通过、Cache 测试失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~发布模式"`
Expected: `发布模式Builder为IsPublishMode` PASS（若 FAIL，按 13.4.6 实际 publish 触发方式调整 `Args`——这是后续所有 publish 测试的前提）；Cache 测试 FAIL。

- [ ] **Step 3: 改 Cache 贡献器加 publish 分支**

替换 `CacheResourceContributor.Contribute`：

```csharp
public void Contribute(GirvsOrchestrationContext context)
{
    var resourceName = context.Options.UseIsolatedCache
        ? $"girvs-cache-{context.Project.Resource.Name}"
        : "girvs-cache";

    if (context.Builder.ExecutionContext.IsPublishMode)
    {
        var external = context.GetOrAddResource(
            resourceName,
            () => context.Builder.AddConnectionString(resourceName, "girvs-cache-connection-string"));
        context.Project.WithReference(external, connectionName: "girvs-cache");
        return;
    }

    var redis = context.GetOrAddResource(resourceName, () => context.Builder.AddRedis(resourceName));
    context.Project.WithReference(redis, connectionName: "girvs-cache").WaitFor(redis);
}
```

Run: 同 Step 2 filter，Cache 测试转 PASS，回归既有 Run 模式 Cache 测试仍 PASS。

- [ ] **Step 4: 改 EventBus 贡献器加 publish 分支**

为 `rabbitmq`/`redis` 分支加 `IsPublishMode` 判断：publish 用 `AddConnectionString(name, "<name>-connection-string")`，run 用原 `AddRabbitMQ`/`AddRedis`；run 模式保留 `WaitFor`，publish 模式不 `WaitFor`。先加对应失败测试（`发布模式EventBusRabbitMQ_创建外部连接串引用而非容器`，断言无 `RabbitMQServerResource`、有名为 `girvs-eventbus-rabbitmq` 的 `IResourceWithConnectionString`），再实现，再确认通过。（统一资源类型形态的写法若别扭，用 publish/run 两条独立分支各自 `WithReference`，以行为为准。）

- [ ] **Step 5: 改 Database 贡献器加 publish 分支**

在 `foreach` 内 `switch (useDataType)` 前，加 `IsPublishMode` 短路：

```csharp
if (context.Builder.ExecutionContext.IsPublishMode)
{
    var external = context.GetOrAddResource(
        resourceName,
        () => context.Builder.AddConnectionString(resourceName, $"{resourceName}-connection-string"));
    context.Project.WithReference(external, connectionName);
    continue;
}
```

先加失败测试（`发布模式数据库_创建外部连接串引用而非容器`，断言无 `MySqlServerResource`、有名为 `girvs-db-order-api-default` 的外部连接串引用），再实现，再确认。

- [ ] **Step 6: 整体单测回归**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 全绿（run 模式与 publish 模式测试并存通过）。

- [ ] **Step 7: 对参照系统生成 publish 清单并人工审查**

为参照系统的 publish 提供外部连接串参数（`Sample.AppHost/appsettings.json` 的 `Parameters` 节，填测试用连接串占位）。
Run（用 dotnet run 传 publish 参数，因 `aspire` CLI 不可用）：`dotnet run --project samples/Sample.AppHost -- --operation publish --publisher manifest --output-path ./samples/publish-out`
Expected：生成的清单中——
1. `service-a`/`service-b`/worker 为项目/容器部署单元；
2. `girvs-cache`/`girvs-mysql`/`girvs-eventbus-rabbitmq` 不再是容器资源，而是外部连接串引用（值取自 `Parameters`）；
3. 敏感参数（如 `jwt-secret`）在清单中以 secret 形式体现。
人工核对无误即可（真集群部署留待业务迁移计划 C）。

- [ ] **Step 8: 提交**

```bash
git add Girvs.Aspire.Hosting/ tests/Girvs.Aspire.Hosting.Tests/ samples/
git commit -m "feat: 生产资源外部引用能力 + 参照系统 publish 清单验证"
```

---

### Task 6: 补齐 Worker 服务 + 收尾文档

**目标交付物**：参照系统补一个 Worker（后台服务）证明 `AddGirvsProject` 对非 Web 项目同样适用；更新 `docs/aspire/apphost-guide.md` 把参照系统作为权威模板指引。

**Files:**
- Create: `samples/Sample.Worker/*`（Worker 项目 + 根模块）
- Modify: `samples/Sample.AppHost/Program.cs`（加 `AddGirvsProject` for worker）
- Modify: `samples/GirvsAspireSample.slnx`
- Modify: `docs/aspire/apphost-guide.md`（补"参照实现"指引与共享配置/生产资源两节）
- Create: `samples/README.md`（如何 `dotnet run` 跑起参照系统、各服务自检端点清单）

**Interfaces:**
- Consumes: 全部前序能力。
- Produces: Worker 作为 `ProjectResource` 被编排；文档闭环。

- [ ] **Step 1: 创建 Worker 项目**

`samples/Sample.Worker/Sample.Worker.csproj`（`Microsoft.NET.Sdk.Worker`，引用 `Girvs`、`Girvs.Aspire`、`Girvs.EventBus`），一个 `BackgroundService` 周期性发一条 CAP 消息或写一次缓存；根模块 `Sample.Worker.WorkerModule` 声明其组件依赖。

- [ ] **Step 2: AppHost 编排 Worker**

`Sample.AppHost/Program.cs` 加 `builder.AddGirvsProject<Projects.Sample_Worker>("worker", typeof(WorkerModule));`；slnx 加入 Worker 项目。

- [ ] **Step 3: 端到端验证**

Run: `dotnet run --project samples/Sample.AppHost`
Expected: Dashboard 出现 `worker` 且 Running，其日志显示后台任务在执行，Traces 能看到 worker 触发的组件调用。

- [ ] **Step 4: 写 samples/README.md 与更新 apphost-guide**

`samples/README.md`：一句话说明这是框架的活体参照实现、`dotnet run --project samples/Sample.AppHost` 启动、列出各服务自检端点（`/health`、`/selfcheck/cache`、`/selfcheck/eventbus`、`/callb`、`/selfcheck/config`）及预期结果。
`docs/aspire/apphost-guide.md` 末尾追加：指向 `samples/` 参照实现作为权威模板 + 共享配置用法 + 生产资源外部引用参数命名表（内容同前述设计）。

- [ ] **Step 5: 全量验证 + 提交**

Run: `dotnet build samples/GirvsAspireSample.slnx` 且 `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 构建成功、测试全绿。

```bash
git add samples/ docs/aspire/apphost-guide.md
git commit -m "docs: 补齐 Worker 样例与参照实现使用文档"
```

---

## Self-Review

**Spec 覆盖**（对照 `2026-07-14-aspire-k8s-production-design.md` 第 0 节实施策略 + 3.2/3.3）：
- 第 0 节"参照实现优先、六步推进" → Task 1（骨架）、Task 2（组件）、Task 3（服务发现）、Task 4（共享配置）、Task 5（外部资源+publish）、Task 6（Worker+文档）✅
- 3.2 共享配置注入（非敏感/敏感拆分、注入全体、共享>本地覆盖）→ Task 4（单测 + 参照系统真实覆盖验证）✅
- 3.3 生产资源外部引用（IsPublishMode 分支、外部连接串参数）→ Task 5（单测 + publish 清单审查）✅
- 3.2 中"SharedConfigurationModule 插高优先级 provider" → 有意偏离为"环境变量注入 + ASP.NET Core 默认环境变量源覆盖"，服务端零改动（理由见设计文档第 0 节与计划 Architecture），并在 Task 4 Step 7 用真实系统证明覆盖生效。
- 3.1 网关 K8s 动态发现 → 不在本计划，属计划 2（参照系统的网关本计划只留静态占位/不建，计划 2 再叠加）。

**Placeholder 扫描**：无 TBD/TODO。服务端启动/组件 API 处均显式标注"以框架真实 API/真跑通为准"（这是诚实的校正点，非占位符——因参照实现是消费框架、需匹配其真实消费契约）。Aspire publish 专属 API 标注核实点，属设计文档已登记风险。

**类型一致性**：`GirvsSharedConfiguration` 成员、`AddGirvsSharedConfiguration`/`GetShared`、`ApplySharedConfiguration`/`ToEnvKey`、外部连接串参数命名（`girvs-cache-connection-string` 等）、资源注入名（`girvs-cache`/`girvs-db-<name>`）跨 Task 4/5 与设计文档 Global Constraints 一致。

## 后续计划
- **计划 2**：`Girvs.Aspire.Gateway`（K8s watch/informer + YARP `IProxyConfigProvider` + 双网关 Label 分流 + RBAC），在本参照系统上叠加网关。
- **计划 3**：NewOnlineRegistration 20+ 服务迁移，照本参照实现改。
