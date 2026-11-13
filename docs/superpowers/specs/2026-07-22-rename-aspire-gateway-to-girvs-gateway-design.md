# Girvs 网关项目改名设计

## 背景

`Girvs.Aspire.Gateway` 当前已经不只是 Aspire AppHost 的本地网关适配层。它同时支持 `GatewayDiscoveryType.Aspire`、`GatewayDiscoveryType.Consul` 和 `GatewayDiscoveryType.Kubernetes`，核心能力是基于不同发现源生成 YARP 反向代理配置，并承载网关侧流程防护等通用能力。

继续使用 `Girvs.Aspire.Gateway` 作为项目名、程序集名和命名空间，会让使用者误以为该包只能在 Aspire 编排下使用；实际在没有 Aspire 编排、只使用 Consul 的部署中也可以工作。因此模块需要改名为通用网关包。

## 已确认方案

采用方案 A：完整改名，保留现有 API 类型与行为。

- 项目名改为 `Girvs.Gateway`。
- 主命名空间改为 `Girvs.Gateway`。
- 测试项目改为 `Girvs.Gateway.Tests`。
- 目录、`.csproj`、解决方案引用、项目引用和文档引用同步更新。
- `AddGirvsGateway`、`GatewayDiscoveryConfig`、`GatewayDiscoveryType`、`AspireGatewayServiceSource`、`ConsulGatewayServiceSource`、`KubernetesGatewayServiceSource` 等类型名保持不变。
- 不增加旧命名空间兼容层。

## 目标

1. 让模块名称准确表达通用网关能力，而不是绑定 Aspire。
2. 保持现有运行行为不变：Aspire、Consul、Kubernetes 三种发现源继续可用。
3. 保持公开 API 的业务语义稳定，只调整命名空间与程序集边界。
4. 更新样例、测试和文档，使仓库内不再把该网关包描述为 Aspire 专属模块。

## 非目标

- 不重命名 `AddGirvsGateway`、`GirvsGatewayProxyConfigProvider` 等网关 API。
- 不修改路由生成规则、发现源实现、配置结构或默认发现源。
- 不删除 Consul 兼容能力。
- 不提供 `Girvs.Aspire.Gateway` 到 `Girvs.Gateway` 的类型转发或包装兼容。
- 不调整 NuGet 版本策略之外的发布流程。

## 改名范围

### 项目与解决方案

- `Girvs.Aspire.Gateway/` 改为 `Girvs.Gateway/`。
- `Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj` 改为 `Girvs.Gateway/Girvs.Gateway.csproj`。
- `tests/Girvs.Aspire.Gateway.Tests/` 改为 `tests/Girvs.Gateway.Tests/`。
- `tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj` 改为 `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj`。
- `Girvs.slnx` 中的项目路径同步更新。
- 样例项目 `samples/Sample.Gateway` 和 `samples/gateway-k8s` 中的 `ProjectReference` 同步更新。

### 命名空间

主命名空间从：

```csharp
Girvs.Aspire.Gateway
Girvs.Aspire.Gateway.Configuration
Girvs.Aspire.Gateway.Discovery
Girvs.Aspire.Gateway.FlowProtection
```

改为：

```csharp
Girvs.Gateway
Girvs.Gateway.Configuration
Girvs.Gateway.Discovery
Girvs.Gateway.FlowProtection
```

测试命名空间从 `Girvs.Aspire.Gateway.Tests` 改为 `Girvs.Gateway.Tests`。

### 程序集和包元数据

`Girvs.Gateway.csproj` 的程序集名、根命名空间和包标识使用 `Girvs.Gateway`。`InternalsVisibleTo` 改为 `Girvs.Gateway.Tests`，确保内部测试继续可访问。

如果当前项目没有显式 `PackageId`，则让 SDK 默认从项目文件名生成 `Girvs.Gateway`；如果存在显式元数据，则同步改名。

### 文档

更新以下文档中的模块名和路径引用：

- 根级项目说明和 `CLAUDE.md`。
- `Girvs.Gateway/README.md`。
- `docs/aspire/apphost-guide.md`。
- `docs/aspire/升级方案.md`。
- 与网关相关的 superpowers 设计文档和计划文档。

历史设计文档中涉及已经完成或废弃方向的内容只做名称一致性修正，不改变其原始技术结论。

## 迁移影响

使用方需要把引用从旧项目或旧包：

```xml
<ProjectReference Include="..\..\Girvs.Aspire.Gateway\Girvs.Aspire.Gateway.csproj" />
```

改为：

```xml
<ProjectReference Include="..\..\Girvs.Gateway\Girvs.Gateway.csproj" />
```

代码中的 using 从：

```csharp
using Girvs.Aspire.Gateway;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
```

改为：

```csharp
using Girvs.Gateway;
using Girvs.Gateway.Configuration;
using Girvs.Gateway.Discovery;
```

配置节名仍保持 `GatewayDiscovery`、`GatewayDiscoveryConfig` 等现状，避免无意义的运行时配置迁移。

## 测试策略

1. 运行网关测试：

```bash
dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --no-restore --nologo
```

2. 运行 Hosting 契约测试，确认样例项目引用和 AppHost 网关配置仍正确：

```bash
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo
```

3. 构建解决方案：

```bash
dotnet build Girvs.slnx --no-restore --nologo
```

4. 运行空白和行尾检查：

```bash
git diff --check
```

## 完成标准

- 仓库内有效源码、测试和示例不再引用 `Girvs.Aspire.Gateway` 项目路径或命名空间。
- `Girvs.slnx` 可以解析新的 `Girvs.Gateway` 和 `Girvs.Gateway.Tests` 项目。
- `Aspire`、`Consul`、`Kubernetes` 三种发现源测试仍通过。
- 样例网关仍通过 `AddGirvsGateway` 注册，配置节和运行行为不变。
- 文档明确 `Girvs.Gateway` 是通用网关包，可在 Aspire、Consul 或 Kubernetes 发现源下使用。
