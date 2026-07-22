# Sample.ServiceA Refit 样例实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Sample.ServiceA 增加构造器注入式 Girvs.Refit 跨服务调用样例。

**Architecture:** 接口声明 ServiceB 的 `/ping` 相对路由并标记 `ServiceDiscovery`；RefitModule 自动注册接口，控制器注入接口并返回调用结果。原生 HttpClientFactory 端点保持不变。

**Tech Stack:** .NET 10、Girvs.Refit、Refit、Aspire 服务发现。

## 全局约束

- 服务名固定为 AppHost 已声明的 `service-b`。
- 接口和控制器不得包含 Consul/Aspire 分支；发现提供者仅由 Refit 配置决定。
- 保留 `/selfcheck/callb`，新增 `/selfcheck/refit-callb`。

### Task 1: 添加 Refit 调用契约和自检端点

**Files:**
- Modify: `samples/Sample.ServiceA/Sample.ServiceA.csproj`
- Create: `samples/Sample.ServiceA/Clients/IServiceBRefit.cs`
- Modify: `samples/Sample.ServiceA/Controllers/SelfCheckController.cs`

- [ ] **Step 1: 添加项目引用**

在 `Sample.ServiceA.csproj` 的 ProjectReference ItemGroup 中加入：

```xml
<ProjectReference Include="..\\..\\Girvs.Refit\\Girvs.Refit.csproj" />
```

- [ ] **Step 2: 添加 Refit 接口**

创建 `Clients/IServiceBRefit.cs`：

```csharp
using Refit;

namespace Sample.ServiceA.Clients;

[RefitService("service-b", RefitServiceAddressType.ServiceDiscovery)]
public interface IServiceBRefit : IGirvsRefit
{
    [Get("/ping")]
    Task<string> PingAsync();
}
```

- [ ] **Step 3: 注入接口并添加端点**

将控制器主构造器改为接收 `IServiceBRefit serviceBRefit`。新增：

```csharp
[HttpGet("refit-callb")]
public async Task<IActionResult> RefitCallB()
{
    var pong = await serviceBRefit.PingAsync();
    return Ok(new { fromServiceB = pong });
}
```

- [ ] **Step 4: 构建验证**

Run: `dotnet restore samples/Sample.ServiceA/Sample.ServiceA.csproj --nologo`

Expected: 还原成功。

Run: `dotnet build samples/Sample.ServiceA/Sample.ServiceA.csproj --no-restore --nologo`

Expected: 成功，零错误。

- [ ] **Step 5: 提交**

```bash
git add samples/Sample.ServiceA/Sample.ServiceA.csproj samples/Sample.ServiceA/Clients/IServiceBRefit.cs samples/Sample.ServiceA/Controllers/SelfCheckController.cs
git commit -m "feat: 新增 ServiceA Refit 调用样例"
```

