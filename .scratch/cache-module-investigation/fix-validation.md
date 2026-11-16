# Redis 自锁修复验证

日期：2026-11-16。

已按主公确认的最小方案完成修复：GirvsCacheModule 删除两个 ShareRedisConnection 调用、辅助方法及相关 using；保留包装器原有连接创建逻辑与其他缓存行为。默认由包装器和 IDistributedCache 分别创建、管理连接。

## 回归测试

文件：`tests/Girvs.Aspire.Hosting.Tests/CacheModuleConnectionTests.cs`。三个 Theory、各覆盖两种 Redis 模式，共新增 6 个用例。

- 无效连接配置下，包装器异步操作及时返回 ArgumentException；随后检查同步入口及同步内存模式管理器首次解析，避免原工厂自锁直接阻塞测试线程。
- 无效连接配置下，IDistributedCache.GetAsync 及时返回配置异常。
- 宿主自定义连接工厂能返回数据库，包装器同步、异步获取均可成功；同步内存缓存能够完成初始化及实际内存读写。仅外部 Redis SDK 使用 DispatchProxy 替身，模块与缓存实现均使用真实代码。

先执行第一组新增用例，修复前两种模式均失败：预期 ArgumentException，实际因自锁产生 TimeoutException。删除回调后两项均通过，再补充其他入口及成功路径验证。

## 验证命令与结果

```powershell
dotnet test tests\Girvs.Aspire.Hosting.Tests\Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~CacheModuleConnection --no-restore --verbosity minimal
```

net10.0 与 net11.0 各 8/8 通过，包含既有的两项 ConnectionRef 测试。

```powershell
dotnet test tests\Girvs.Aspire.Hosting.Tests\Girvs.Aspire.Hosting.Tests.csproj --no-restore --verbosity minimal
```

net10.0 与 net11.0 各 58/58 通过，无失败或跳过。

```powershell
dotnet build Girvs.Cache\Girvs.Cache.csproj --no-restore -p:GeneratePackageOnBuild=false --verbosity minimal
```

net8.0、net9.0、net10.0、net11.0 均成功；0 警告、0 错误。

```powershell
dotnet run --project .scratch\cache-module-investigation\probe\Probe.csproj -f net11.0 --no-restore --verbosity quiet
```

原始诊断程序的三条自锁断言均通过；剩余 5 条失败对应三项未纳入修复的遗留问题。因此该综合探针退出码仍为 1。详见 [修复后输出](net11-fixed-output.txt)。

完整 Hosting 测试输出含既有 Aspire.Hosting 13.4.6 的 NU1903 警告，本次未变更依赖。未进行真实 Redis 连接、Redis 服务端读写、集群或网络断线测试。

## Standards

主公，Standards 轴无发现：硬性规范违规 0 项，需处理的代码异味 0 项。

生产修改仅删除共享回调，范围集中；新增测试通过公开接口调用真实模块，仅替代外部 Redis SDK 接口，符合 tdd 的测试原则。测试使用既有 CacheModuleCollection 隔离全局配置，并在释放阶段恢复 Singleton<AppSettings>、清理容器及内存缓存。无效配置和超时保护可实际捕获原自锁，未引入真实 Redis 依赖。

## Spec

Spec 轴：未发现问题。

- 符合规格“移除两个 Redis 分支中的连接共享回调及其辅助方法”：删除完整，默认连接路径不再回调包装器自身，原有包装器逻辑保留。
- 符合“本次不处理其余三项遗留问题”：生产改动仅限自锁修复，无越界变更。
- 测试覆盖规格列出的公开入口：同步、异步取连接，IDistributedCache，同步内存缓存解析及宿主自定义工厂。异常配置与超时断言能够捕获旧自锁；异步预检失败后不会进入阻塞的同步路径。

验证边界明确：尚未验证真实 Redis 的成功连接与读写，这不影响本次自锁回归的证据。

Standards 发现 0 项；Spec 发现 0 项，均无阻塞问题。
