# .NET 11 升级前后归因

核查日期：2026-11-16。此处比较 Git 历史源码，未重新构建完整旧版本。

## 比较基线

- 升级前最后一个提交：`bb7ede63972dbc16b41fef60f57176617305ebc4`，2026-11-13 11:48:26（UTC+8）。
- 首次新增 .NET 11：`4b4db8640bb1ad751649280e25044aadafb7fa33`，2026-11-13 12:35:39。
- 为 Girvs.Cache 补充 net11.0 依赖：`ae7aac48365830b8596addedf97917b974b5eac3`，2026-11-13 13:48:10。
- 引入连接共享优化：`472821798bdae669bbcd774cb3b7db62e438e82d`，2026-11-16 12:12:38。

## 结果

| 问题 | 升级前是否存在 | 归因 |
| --- | --- | --- |
| Redis 默认连接工厂回调自身，首次连接自锁 | 不存在这条自锁路径 | 11 月 16 日的 `4728217` 新增 `ShareRedisConnection` 后引入 |
| 不支持的资源类型留下不完整注册 | 已存在 | 升级前 switch 无 default；`04f74dd` 于 2026-07-22 注释 SQL Server 分支后，sqlserver 也落入该情况 |
| 内存缓存覆盖宿主配置 | 已存在 | 内存回退分支的实例注册来自 `40cf538`（2025-01-18）；同步内存分支来自 `864976c`（2026-07-17） |
| 模块创建的内存缓存未随容器释放 | 已存在 | 与上述实例注册方式同源 |

升级前模块仅设置 Redis 的 Configuration 和 InstanceName，未设置 ConnectionMultiplexerFactory。包装器创建连接时默认直接调用 ConnectionMultiplexer.Connect/ConnectAsync，不会经模块注入的工厂回调自身。

## 升级提交未改变这两个实现文件

执行：

```powershell
git diff bb7ede6 ae7aac4 -- Girvs.Cache/GirvsCacheModule.cs Girvs.Cache/CacheImps/RedisConnectionWrapper.cs
```

输出为空。文件对象哈希也逐一一致：

| 文件 | bb7ede6 与 ae7aac4 共同的 Git blob |
| --- | --- |
| GirvsCacheModule.cs | `1d1dfae0fd8facb1dcf45ab77ca93c4df4eeebdb` |
| RedisConnectionWrapper.cs | `a74b01c00915279666a291779fe395be414284c5` |

之后 `4728217` 在两种 Redis 模式中调用 ShareRedisConnection，并让该工厂调用同一个 RedisConnectionWrapper.GetConnectionMultiplexerAsync；这是本次自锁路径新增的关键。

当前代码已在 .NET 10.0.9 和 .NET 11.0.0 上复现相同自锁，见 [诊断报告](report.md)。结合提交差异，可确定该问题由升级后的应用代码优化引入，不能归因为 .NET 11 运行时兼容性变化。

建议优先针对连接共享回调修复；仅将当前代码的目标框架改回 net10.0 不能消除自锁。其余三项应按遗留问题处理。
