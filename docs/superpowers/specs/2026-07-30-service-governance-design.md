# Girvs.ServiceGovernance 服务治理设计

## 状态

- 日期：2026-07-30
- 状态：已冻结
- 本设计推翻 `2026-07-21-remove-consul-aspire-only-design.md` 中“彻底移除 Consul”的决策。

## 背景

当前仓库分别通过 `Girvs.Aspire` 和 `Girvs.Consul` 提供 Aspire 服务发现、可观测性与 Consul 服务注册。`Girvs.Refit` 又维护了独立的 Aspire/Consul 客户端发现开关，容易出现服务注册端与调用端配置不一致。

## 目标

1. 将 `Girvs.Aspire` 重命名为 `Girvs.ServiceGovernance`。
2. 在 net10.0 的 Aspire 编排场景中，通过配置互斥选择 Aspire 或 Consul。
3. 保留 `Girvs.Consul`，继续支持 net8.0、net9.0 和传统部署。
4. net10.0 下让 `Girvs.Refit` 使用服务治理模块的统一 Provider 和 Consul 地址。
5. 两种 Provider 模式都保留 OpenTelemetry、Serilog OTLP、`/health` 和 `/alive`。

## 非目标

- 不修改 `Girvs.Consul`。
- 不修改 `Girvs.Aspire.Hosting` 和 `Girvs.Gateway`。
- 不为 ServiceGovernance 支持 net8.0/net9.0。
- 不支持 Consul `Mvc` 注册模式。

## 项目与类型命名

| 原名称 | 新名称 |
| --- | --- |
| `Girvs.Aspire` | `Girvs.ServiceGovernance` |
| `AspireModule` | `ServiceGovernanceModule` |
| `GirvsAspireSerilogHook` | `GirvsSerilogOtlpHook` |
| `Girvs.Aspire.Tests` | `Girvs.ServiceGovernance.Tests` |

`Girvs.Aspire.Hosting` 保持原名。

## 配置模型

配置遵循 Girvs 既有模块配置绑定约定：

```json
{
  "ModuleConfigurations": {
    "ServiceGovernanceConfig": {
      "ServiceDiscoveryProvider": "Aspire",
      "ConsulAddress": "http://127.0.0.1:8500",
      "HealthAddress": "http://127.0.0.1/health",
      "ServerName": "",
      "Interval": 10,
      "DeregisterCriticalServiceAfter": 90,
      "Timeout": 30,
      "CurrentServerModel": "WebApi"
    }
  }
}
```

### ServiceDiscoveryProvider

- `Aspire`：启用 `AddServiceDiscovery` 和 HttpClient 服务发现，不向 Consul 注册。
- `Consul`：不启用 Aspire 服务发现；向 Consul 注册服务；net10.0 下 Refit 查询 Consul 健康实例。

### ConsulServerModel

- `WebApi`：向 Consul 注册 HTTP 健康检查，地址使用 `HealthAddress`。
- `GrpcService`：向 Consul 注册 gRPC 健康检查，并注册 `IConsulClient` 单例。

## 运行时架构

### ServiceGovernanceModule

- 实现 `IAppModuleStartup`，`Order = -10000`。
- 所有模式注册 `ServiceGovernanceConfig`、健康检查和可选 OpenTelemetry。
- Aspire 模式注册 .NET Service Discovery。
- Consul 模式注册内部 `IConsulServiceRegistrar`。
- `Configure` 阶段执行 Consul 注册，异常记录结构化日志但不阻断启动。
- 始终映射 `/health` 和 `/alive`。

### ConsulServiceRegistrar

- 模块内部实现，不依赖 `Girvs.Consul` 项目。
- 负责创建 WebApi/gRPC `AgentServiceRegistration`、注册和停止时注销。
- 注销失败仅记录日志。
- WebApi 模式直接持有 ConsulClient；gRPC 模式使用 DI 中的 `IConsulClient`。

### gRPC Health

`HealthCheckService` 实现 `Grpc.Health.V1.Health.HealthBase` 和 `IAppGrpcService`。由于现有 `GrpcModule` 会无条件扫描 `IAppGrpcService`，只要引用 ServiceGovernance 就始终映射标准 gRPC Health 服务。

### Serilog OTLP 反射契约

Girvs 核心反射目标更新为：

```text
Girvs.ServiceGovernance.GirvsSerilogOtlpHook, Girvs.ServiceGovernance
```

方法签名保持：

```csharp
public static void AddOtlpSink(LoggerConfiguration loggerConfiguration)
```

## Girvs.Refit 集成

- `Girvs.Refit` 仅在 net10.0 条件引用 `Girvs.ServiceGovernance`。
- net10.0 下 Resolver 由 `ServiceGovernanceConfig.ServiceDiscoveryProvider` 选择。
- Consul Resolver 优先使用 `ServiceGovernanceConfig.ConsulAddress`，为空时回退 `RefitConfig.GetConsulAddress()`。
- net8.0/net9.0 继续使用 `RefitConfig.DiscoveryProvider`，行为不变。
- `RefitDiscoveryProvider` 和 `RefitConfig.DiscoveryProvider` 仅在 net10.0 标记过时。

## 错误处理

- ConsulAddress 为空：记录警告并跳过注册。
- Consul 注册失败：记录错误并继续启动。
- Consul 注销失败：记录错误并继续关闭。
- Refit 查询不到健康实例：保持抛出 `GirvsException`。

## 验收标准

1. Aspire 模式注册 .NET Service Discovery，不注册 Consul 注册器。
2. Consul 模式不注册 Aspire Service Discovery。
3. WebApi 和 gRPC 注册信息包含正确服务名、地址、端口、超时与健康检查。
4. Consul 注册异常不阻断应用启动。
5. 标准 gRPC Health `Check`/`Watch` 返回 `Serving`。
6. Serilog OTLP 新反射契约可加载。
7. net10.0 Refit 根据 ServiceGovernanceConfig 选择 Resolver。
8. Girvs.Refit net8.0/net9.0/net10.0 均构建通过。
9. `Girvs.Consul` 无任何改动。
10. `Girvs.slnx` Release 构建通过。
