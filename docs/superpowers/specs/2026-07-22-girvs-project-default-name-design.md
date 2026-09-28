# AddGirvsProject 默认资源命名设计

## 背景

Girvs 服务运行时的 OpenAPI 网关地址与 Consul 服务名均由程序集名转换为小写连字符格式，例如 `Sample.ServiceA` 生成 `sample-servicea`。现有 AppHost 示例将资源名手写为 `service-a`，使 Aspire 网关路由前缀与 OpenAPI 文档不一致。

## 目标

为 `AddGirvsProject<TProject>` 增加无需传入资源名的重载。该重载从 `TProject` 的生成类型名（如 `Sample_ServiceA`）生成 Aspire 合法的 `sample-servicea`，并使 `Girvs.Consul` 和 `Girvs.OpenApi` 使用同一命名规则。

## 设计

- 新增 `AddGirvsProject<TProject>(this IDistributedApplicationBuilder builder)`。
- 在核心 `Girvs` 中定义公共服务名转换器：程序集名将 `.` 替换为 `-` 并使用 `ToLowerInvariant()`；项目元数据类型名将 `_` 替换为 `-` 并使用 `ToLowerInvariant()`。
- `Girvs.Consul` 的 WebApi 与 gRPC 注册在未显式配置 `ServerName` 时调用程序集名转换器。
- `Girvs.OpenApi` 的网关 Server 地址调用程序集名转换器。
- `AddGirvsProject<TProject>` 无参重载调用项目元数据类型名转换器。
- 新重载调用既有 `AddGirvsProject<TProject>(builder, name)`，因此共享配置、资源等待与发布模式行为不变。
- 保留带 `name` 参数的重载，以支持需要显式自定义资源名的现有调用方，避免破坏性变更。
- Sample AppHost 改用新重载；服务发现 URL 与 Refit 服务名改为新资源名。

## 错误处理与兼容性

类型名中的下划线转换为连字符，避免 Aspire 对资源名中下划线的校验错误。类型名不含下划线时仍转换为小写。显式传入的名称沿用 Aspire 原有校验。

## 测试

在 `Girvs.Aspire.Hosting.Tests` 验证两种输入都生成 `sample-servicea`、无参重载生成该资源名，并确认显式名称重载保持可用；通过受影响模块构建确保 `Girvs.Consul` 与 `Girvs.OpenApi` 复用公共转换器。运行受影响项目的测试和样例构建验证。
