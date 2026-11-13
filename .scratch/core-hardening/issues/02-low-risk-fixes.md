# 02 — 低风险独立修复

Status: completed
Blocked by: 01

## 完成内容

- 删除 `ServiceCollectionExtensions` 中已无效的 `ServicePointManager.SecurityProtocol` 设置。
- `GirvsModuleStartup` 的 `ForwardedHeadersOptions` 按目标框架选择 `KnownIPNetworks`（net10+）或 `KnownNetworks`（net8/net9）。
- 删除 `GirvsException` 的 `[Serializable]` 与 `SerializationInfo` 构造。
- `RequestExtensions` 增加 UA 长度、远端地址空值、IP 串越界保护；`GetServerVariable` 保持空值兜底。
- `HashHelper` 用显式 `switch` 替换 `CryptoConfig.CreateFromName`，保持默认 SHA1 行为并释放算法实例。
- 新增 `CreateGirvsWebApplication`，旧拼写与旧命名方法保留并标记 `Obsolete`；示例与契约测试改用新名。

## 验证结果

- 整仓构建：0 错误。
- 核心库 SYSLIB0051、SYSLIB0014、ASPDEPR005 警告消除。
- `Girvs.Core.Tests` 四目标各 51 项通过。
