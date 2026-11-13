---
artifact_type: spec
artifact_version: 1.0.0
workflow_status: approved
approved_at: 2026-08-17
requirements_report: REQ-2026-08-17-SGHC
---

# Girvs.ServiceGovernance 健康检查与服务名配置模型重构

## 状态

- 日期：2026-08-17
- 状态：已批准（2026-08-17 用户确认）
- 关联需求报告：REQ-2026-08-17-SGHC（已冻结）
- 前置设计：`2026-07-30-service-governance-design.md`（本 Spec 在其基础上演进，不推翻既有决策）
- 相关设计：`2026-07-22-unify-service-naming` 计划（已落地 `ServiceNameResolver`，本 Spec 在其上增加"配置优先"层）

## 1. 背景与目标

现状问题：

1. `ServiceGovernanceConfig.HealthAddress`（完整 URL，默认 `http://127.0.0.1/health`）仅 Consul 模式使用；Aspire 与 K8s 模式的健康检查地址分别散落在 AppHost 代码（`WithHttpHealthCheck`）与 Deployment yaml 探针中，无法同源配置，存在漂移风险（如 Consul 检查通过而 K8s 探针 404）。
2. `ServerName` 定位尴尬：仅 Consul 注册回退使用（配了才用、不配回退程序集名），Aspire 资源名走类名转换，配置层无法统一自定义服务名，Consul 注册名与 Aspire 资源名（服务发现地址）可能不一致。
3. Consul gRPC 健康检查现状 `host:port/health` 中 `/health` 会被 Consul 解释为 gRPC service identifier 而非 HTTP 路径（已由官方文档与源码实证，见"依据"节），语义不规范且依赖服务端忽略 `Service` 参数才能正常工作。

目标：

1. 用一套配置模型统一 Consul / Aspire / K8s 三种模式的服务健康检查路径与地址，配置一处、三处同源。
2. 将 `ServerName` 升级为唯一服务名入口：Aspire 资源名、Consul 注册名、网关 `Services` 元数据 key 同源。
3. 修正 Consul gRPC 检查形态为官方规范。

## 2. 范围内 / 范围外

### 范围内

1. `ServiceGovernanceConfig` 配置模型拆分：删除 `HealthAddress`，新增 `ConsulRegistrationAddress`、`HealthCheckPath`、`LivenessCheckPath`。
2. `ConsulServiceRegistrar` 组合规则改造（WebApi/gRPC 规范组合）。
3. `ServiceGovernanceModule.ConfigureMapEndpointRoute` 端点映射改为配置驱动。
4. `Girvs.Aspire.Hosting/GirvsProjectExtensions`：`WithHttpProbe` 双 probe 接线 + 从服务 appsettings.json 读取健康检查路径与 `ServerName`。
5. `ServerName` 唯一入口化：Aspire 资源名与 Consul 注册名统一归一化。
6. 受影响测试、示例、文档同步迁移（Breaking 变更）。

### 范围外（非目标）

- 不保留 `HealthAddress` 的兼容读取/反解（明确排除）。
- 不调整 gRPC `HealthCheckService` 按 `request.Service` 校验的行为（现状忽略该参数，不在本次范围）。
- 不对 K8s 手写 yaml 的探针做强制校验（仅文档约定）。
- 不改动 `ConsulAddress`（Consul 服务器地址）、`Interval` / `Timeout` / `DeregisterCriticalServiceAfter` / `CurrentServerModel` / `DiscoveryRefreshInterval` / `MaxStaleDuration` / `KubernetesNamespace` / `KubernetesLabelSelector` / `Services` 等现有属性。
- 不把 `ServiceGovernanceConfig` 按模式拆分成嵌套子配置对象（本次仅以 XML 注释标注各属性生效模式；拆分留作后续演进候选）。
- 不修改 `Girvs.Consul`、`Girvs.Gateway` 项目代码。

## 3. 角色与场景

- **服务开发者**：在服务 appsettings.json 中配置健康检查路径、服务名。
- **运维（Consul 部署）**：配置 `ConsulRegistrationAddress`，Consul agent 主动拉取 HTTP/gRPC 检查。
- **运维（Aspire 编排）**：AppHost 自动声明 readiness/liveness probe，dashboard 展示健康状态；publish 到 K8s 生成清单含探针。
- **运维（K8s 手写 yaml）**：按文档约定在 Deployment 中写 `httpGet.path` 与配置一致。

场景矩阵（配置属性 × 模式）：

| 属性 | Consul | Aspire | K8s（手写 yaml） |
| --- | --- | --- | --- |
| `ConsulRegistrationAddress` | 注册地址 + 检查基址 | 忽略（端口用 targetPort） | 忽略（用 Pod IP） |
| `HealthCheckPath` | 拼入 HTTP 检查 URL | `WithHttpProbe(Readiness, path)` | `readinessProbe.httpGet.path` |
| `LivenessCheckPath` | 忽略 | `WithHttpProbe(Liveness, path)` | `livenessProbe.httpGet.path` |
| `ServerName`（归一化后） | Consul 注册名 | AppHost 资源名 = 服务发现名 | （同 Aspire publish 或显式配置） |

## 4. 功能需求

### FR-1 配置模型

`ServiceGovernanceConfig` 变更：

- 删除 `HealthAddress`。
- 新增 `ConsulRegistrationAddress`（`string?`，默认 `http://127.0.0.1`）——仅 Consul 模式生效；服务对外注册基址（host:port），不含路径。
- 新增 `HealthCheckPath`（`string`，默认 `/health`）——完整健康验证路径（readiness/部署后验证）；Consul HTTP 检查、Aspire Readiness probe、K8s readinessProbe、框架端点映射共用。
- 新增 `LivenessCheckPath`（`string`，默认 `/alive`）——存活探测路径；Aspire Liveness probe、K8s livenessProbe、框架端点映射使用；Consul 模式忽略。
- 每个属性带 XML 注释标注生效模式。

可度量标准：编译通过；`ServiceGovernanceConfigTests` 断言三个默认值。

### FR-2 Consul WebApi 组合规则

`ConsulServiceRegistrar.CreateWebApiRegistration`：

- 注册 `Address` / `Port` 从 `ConsulRegistrationAddress`（`Uri`）解析。
- HTTP check = `ConsulRegistrationAddress`（去尾部 `/`）+ `HealthCheckPath`（如 `http://127.0.0.1:8080/health`）。

可度量标准：单测断言注册对象的 `Address`、`Port`、`Check.HTTP` 精确值。

### FR-3 Consul gRPC 组合规则

`ConsulServiceRegistrar.CreateGrpcRegistration`：

- 注册 `Address` / `Port` 同上。
- GRPC check = `ConsulRegistrationAddress` 去掉 `scheme://` 后的纯 `host:port`，**不含 `HealthCheckPath`**。

可度量标准：单测断言 `Check.GRPC == "127.0.0.1:5080"`（当 `ConsulRegistrationAddress = http://127.0.0.1:5080` 时）。

### FR-4 端点映射配置驱动

`ServiceGovernanceModule.ConfigureMapEndpointRoute`：

```csharp
builder.MapHealthChecks(config.HealthCheckPath);
builder.MapHealthChecks(config.LivenessCheckPath, new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
});
```

可度量标准：默认值下端点仍为 `/health`、`/alive`（与现状等价）；自定义 path 时端点随之变化（单测断言映射 pattern 包含配置值）。

### FR-5 Consul 空注册地址行为

Consul 模式下 `ConsulRegistrationAddress` 为空或非法 URI：

- 为空：记录警告日志并跳过注册（与 `ConsulAddress` 空值处理一致）。
- 非法 URI：抛出 `GirvsException`，启动失败（显式暴露配置错误）。

可度量标准：日志/异常单测断言。

### FR-6 Aspire probe 接线

`GirvsProjectExtensions.AddGirvsProject`（含 publish 分支）为每个 project 资源声明：

```csharp
project.WithHttpProbe(ProbeType.Readiness, healthCheckPath);
project.WithHttpProbe(ProbeType.Liveness, livenessCheckPath);
```

- `healthCheckPath` / `livenessCheckPath` 从服务 appsettings.json 读取（缺省回退 `/health`、`/alive`）。
- 端口不显式指定，由 Aspire 自动取 endpoint targetPort。
- 依赖：`WithHttpProbe` 需 Aspire 9.5+；当前 `Aspire.Hosting` 13.4.6 满足。

可度量标准：Hosting 测试断言 probe 声明；Aspire publish 清单含 readiness/liveness probe 且 path 与配置一致。

### FR-7 Aspire 资源名读取 ServerName

`AddGirvsProject<TProject>()` 无参重载：

1. 通过 `project.Resource.GetProjectMetadata().ProjectPath` 定位服务 csproj 目录；
2. 读取同目录 `appsettings.json` 的 `ModuleConfigurations:ServiceGovernanceConfig:ServerName`；
3. 非空 → 经归一化后作为资源名；为空/文件不存在/节点缺失 → 回退 `ServiceNameResolver.FromProjectMetadataName(typeof(TProject).Name)`（现状行为）。

显式传名 `AddGirvsProject<TProject>(name)` 优先级最高，行为不变。

可度量标准：Hosting 测试——配 ServerName 用配置名；未配用类名转换名；显式传名覆盖一切。

### FR-8 Consul 服务名归一化

`ConsulServiceRegistrar.GetServerName`：

- `ServerName` 非空 → `ServiceNameResolver` 归一化后使用（现状为原样使用，本次修正）；
- 为空 → 回退 `ServiceNameResolver.FromAssemblyName()`（现状不变）。

可度量标准：单测断言归一化结果（如 `Sample.ServiceA → sample-servicea`）。

## 5. 非功能需求

- Breaking 变更，但默认值下运行行为与现状等价（除 FR-3 gRPC 检查形态、FR-8 非规范 `ServerName` 归一化）。
- 无新增包依赖；所有改动基于现有 `Girvs.ServiceGovernance`、`Girvs.Aspire.Hosting`、`Girvs` 项目。
- 受影响测试项目（`Girvs.ServiceGovernance.Tests`、`Girvs.Aspire.Hosting.Tests`）全部更新并通过。
- 服务名与健康检查路径统一遵循小写连字符规则（`ServiceNameResolver`），杜绝格式漂移。

## 6. 接口 / 数据 / 安全约束

### 配置模型（appsettings.json）

```json
{
  "ModuleConfigurations": {
    "ServiceGovernanceConfig": {
      "ServiceDiscoveryProvider": "Aspire",
      "ConsulRegistrationAddress": "http://127.0.0.1",
      "HealthCheckPath": "/health",
      "LivenessCheckPath": "/alive",
      "ServerName": ""
    }
  }
}
```

- 绑定机制沿用现有 `ModuleConfigurations:{TypeName}` 约定（`ServiceCollectionExtensions` 绑定），无新增机制。
- `ConsulRegistrationAddress` 必须为合法绝对 URI（`http`/`https`）；`HealthCheckPath` / `LivenessCheckPath` 必须以 `/` 开头。
- **读取边界**：AppHost 仅能读取服务项目目录的 `appsettings.json` 文件配置；由环境变量 / ConfigMap 注入的 `ServerName` 或健康检查路径 AppHost 读取不到，此时由 `AddGirvsProject<T>(name)` 显式传名兜底。

### 归一化规则

`ServiceNameResolver` 新增服务名归一化方法（如 `FromServerName`），规则与既有方法一致：

- `.` 与 `_` → `-`；
- 全部小写化；
- 对已是"小写字母、数字、连字符"的规范配置幂等（无 `.`、`_`、大写时结果不变）。

### 安全约束

- 健康检查端点仅暴露存活/就绪信息，不返回敏感数据（沿用现有 `HealthCheckOptions` 行为）。
- 不新增密钥、连接串相关配置。

## 7. 失败与边界行为

| 场景 | 行为 |
| --- | --- |
| Consul 模式 `ConsulRegistrationAddress` 为空 | 告警日志，跳过注册（FR-5） |
| `ConsulRegistrationAddress` 非法 URI | 抛 `GirvsException`，启动失败（FR-5） |
| 服务 appsettings.json 不存在或节点缺失 | Aspire 侧回退默认（`/health`、`/alive`、类名服务名） |
| `ServerName` 为非规范格式（含大写/点号/下划线） | 统一归一化，幂等规则保证规范配置不受影响 |
| K8s 手写 yaml 探针 path 与配置不一致 | 框架无法强制；文档约定默认 `/health`、`/alive` 与配置驱动映射保证框架侧一致 |
| Consul 注册失败 / 注销失败 | 记录结构化日志，不阻断启动/关闭（现状行为，不变） |

## 8. 验收标准

1. `dotnet build Girvs.slnx` 与 `dotnet test Girvs.slnx` 全部通过。
2. FR-1~FR-8 均有对应单测且断言精确。
3. `Girvs.Aspire.Hosting.Tests` 覆盖：ServerName 读取/回退/显式覆盖；probe 声明。
4. Aspire publish 清单包含 readiness/liveness probe，path 与配置一致。
5. 全仓库（含 tests、samples、docs）无 `HealthAddress` 残留引用。
6. `ServiceNameResolver` 新增归一化用例覆盖规范/非规范输入。
7. 默认配置下 Consul 注册的 WebApi/gRPC 检查与端点映射行为与现状等价（除 FR-3/FR-8 明确修正项）。

## 9. 风险与依据

| 风险 | 等级 | 缓解 |
| --- | --- | --- |
| gRPC 检查形态变化（`host:port/health` → `host:port`） | 低 | 官方规范修正；单测覆盖 |
| 现有含 `HealthAddress` / 非规范 `ServerName` 的配置失效 | 中 | Breaking 已确认；迁移说明写入文档与 PR 描述 |
| AppHost 读不到非文件型配置 | 低 | 显式传名重载兜底（已有） |
| 自定义 path 后手写 yaml 探针不一致 | 中 | 配置驱动映射 + 文档约定 |
| `WithHttpProbe` 依赖 Aspire 9.5+ | 低 | 当前 `Aspire.Hosting` 13.4.6 满足；升级校验入测试 |

### 依据（外部一手资料，scout 实证）

1. **Consul gRPC 检查格式**：`GRPC` 字段合法格式为 `host:port` 或 `host:port/:service_identifier`，`/health` 被解释为 gRPC service identifier 而非 HTTP 路径；gRPC 探活走标准 `grpc_health_v1.Health/Check`，路径不可自定义。
   - 来源：https://developer.hashicorp.com/consul/api-docs/agent/check#grpc ；https://github.com/hashicorp/consul/blob/v1.7.14/agent/checks/grpc.go
2. **Aspire 健康检查 → K8s 探针转换**：`WithHttpHealthCheck` 不自动生成 K8s 探针（issue #17714）；`WithHttpProbe`（Aspire 9.5+）会被 Kubernetes 发布器转换为 `readinessProbe`/`livenessProbe`/`startupProbe`，映射为 `AppHost path → httpGet.path`、`endpoint targetPort → httpGet.port`。
   - 来源：https://github.com/microsoft/aspire/issues/17714 ；https://github.com/microsoft/aspire/pull/11081 ；https://github.com/microsoft/aspire/blob/v9.5.2/src/Aspire.Hosting.Kubernetes/KubernetesResource.cs

## 10. 影响面（文件清单）

| 文件 | 变更 |
| --- | --- |
| `Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs` | 三属性替换 `HealthAddress`；XML 注释标注生效模式 |
| `Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs` | FR-2/3/5/8 组合规则与归一化 |
| `Girvs.ServiceGovernance/ServiceGovernanceModule.cs` | FR-4 配置驱动映射；FR-5 空地址告警 |
| `Girvs/Infrastructure/ServiceNameResolver.cs` | 新增服务名归一化方法 |
| `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs` | FR-6 probe 接线；FR-7 ServerName/appsettings 读取 |
| `tests/Girvs.ServiceGovernance.Tests/*` | 配置默认值、注册对象、端点映射、归一化断言 |
| `tests/Girvs.Aspire.Hosting.Tests/*` | probe 声明、ServerName 读取/回退/显式覆盖断言 |
| `samples/*`（如 `Sample.AppHost`） | 按需演示 ServerName/健康检查配置 |
| `docs/superpowers/drafts/plans/` | 后续 Implementation Plan（本 Spec 批准后产出） |
