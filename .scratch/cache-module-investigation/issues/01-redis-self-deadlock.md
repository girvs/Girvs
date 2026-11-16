Status: completed
Blocked by: 无

# P1：消除 Redis 连接共享工厂自锁

详见 [诊断报告](../report.md) 第 1 项。

验收：默认 Redis 配置下，首次同步和异步获取连接、IDistributedCache 操作及同步内存缓存的首次解析均不自锁；保留宿主自定义工厂。按主公确认的最小方案撤销默认连接共享，因此共享连接的重连、释放所有权不再属于本次修复范围。

## Comments

2026-11-16：已在 net10.0、net11.0 无网络复现；未修改生产实现。

2026-11-16：主公确认实施最小修复。先新增测试，在两个 Redis 模式下均以 TimeoutException 复现；随后删除两个 ShareRedisConnection 调用及辅助方法，测试转为预期的配置异常。新增 6 个回归用例，Hosting 完整测试在两个框架各 58/58 通过；缓存模块四目标构建通过。规范与需求审查均无发现。详见 [修复验证](../fix-validation.md)。
