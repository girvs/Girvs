# Sample.ServiceA Refit 跨服务调用设计

## 目标

在 `Sample.ServiceA` 提供一个使用 `Girvs.Refit` 调用 `Sample.ServiceB` 的最小可运行示例，演示业务代码通过构造器注入 Refit 接口完成内部服务调用。

## 设计

- 保留现有 `/selfcheck/callb`，继续作为原生 `IHttpClientFactory` 服务发现对照。
- 新增 `IServiceBRefit : IGirvsRefit`，使用 `[RefitService("service-b", RefitServiceAddressType.ServiceDiscovery)]` 和 `[Get("/ping")]` 声明目标服务。
- `Sample.ServiceA` 引用 `Girvs.Refit`。`RefitModule` 自动扫描并注册该接口。
- `SelfCheckController` 构造器注入 `IServiceBRefit`，新增 `GET /selfcheck/refit-callb`，返回 ServiceB 的响应。

## 部署行为

AppHost 已通过 `WithReference(serviceB)` 注入 `service-b` 的 Aspire 服务发现地址。Refit 接口不包含 Consul 或 Aspire 判断；实际发现提供者由 `RefitConfig:DiscoveryProvider` 决定，切换部署无需改动接口或控制器。

## 验证

- 构建 `samples/Sample.ServiceA/Sample.ServiceA.csproj`。
- 单独运行或由 AppHost 编排后，请求 `/selfcheck/refit-callb`，返回 `fromServiceB` 的 ping 内容。
