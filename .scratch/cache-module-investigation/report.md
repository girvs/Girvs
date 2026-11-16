# GirvsCacheModule 诊断报告

初次探查日期：2026-11-16。以下故障证据对应修复前代码。

最新状态：主公确认后已按最小方案修复第 1 项自锁，完整验证见 [fix-validation.md](fix-validation.md)。第 2 至第 4 项保持独立待处理。

补充历史归因：参见 [.NET 11 升级前后比较](history.md)。Redis 自锁由升级后的连接共享优化引入，其余三项在升级前已存在。

已确认 4 项问题，首要问题会令默认 Redis 配置下的首次缓存操作永久等待。独立探针在 .NET 10.0.9 和 .NET 11.0.0 得到相同结果；两次最终运行各有 8 项通过断言、8 项失败断言。8 项失败断言对应下述 4 个问题的不同触发场景。

## 1. P1：共享连接工厂回调自身，形成自锁

位置：`Girvs.Cache/GirvsCacheModule.cs:120`；关联 `Girvs.Cache/CacheImps/RedisConnectionWrapper.cs:52`、`:74`、`:92`、`:126`。

触发条件：分布式缓存启用、引用有效、资源类型为 `redis` 或 `redis-synchronized-memory`，且宿主未提供其他 `ConnectionMultiplexerFactory`。

`ShareRedisConnection` 把工厂设置成“调用包装器获取连接”。包装器同时消费同一份 `RedisCacheOptions`，创建连接时又调用该工厂：

```text
GetConnectionMultiplexerAsync()
  → GetConnectionAsync()，取得 _connectionLock
    → ConnectAsync()
      → options.ConnectionMultiplexerFactory()
        → 同一包装器.GetConnectionMultiplexerAsync()
          → GetConnectionAsync()，等待同一把锁
```

外层等待内层返回，内层等待外层释放锁。延迟解析包装器只避开了 Options 构建阶段的依赖循环，没有消除运行时循环。

实测：第一次取连接出现 `GetConnectionAsync=2, ConnectAsync=1, SemaphoreCount=0`，任务不完成；真实 `IDistributedCache.GetAsync` 同样不完成。同步内存模式会在 `RedisSynchronizedMemoryCache` 构造函数订阅 Redis 时进入同步路径并卡住，因此可能表现为首个请求或启动期间服务解析挂起。

控制实验：Options 和包装器均能同步解析成功；仅把连接工厂替换为立即返回异常的探针工厂，包装器立即收到异常。探针连接串含无效配置项，本应在解析阶段立即报错；自锁发生时连该阶段都未到达。故不依赖 Redis 服务状态，也不是网络超时。

Git 记录：此循环由 `4728217`（`perf: 优化缓存模块连接复用与批量操作`）中的连接共享改动引入。

建议：明确底层连接的创建者，分离“包装器创建连接所用的工厂”与“IDistributedCache 借用连接所用的工厂”，消除回调自身的路径。若继续共享连接，还须验证断线重连与释放的所有权。短期移除共享回调能打断这一循环，但会恢复独立连接，不能视为连接共享方案已完成。

任务：[01-redis-self-deadlock.md](issues/01-redis-self-deadlock.md)。

## 2. P2：不支持的资源类型留下不完整的服务注册

位置：`Girvs.Cache/GirvsCacheModule.cs:33`、`:90`。

`switch` 仅处理两类 Redis 资源，没有 `default`。SQL Server 分支已被注释。只要引用有效，就不会进入内存回退分支；但 `switch` 之后仍注册 `DistributedCacheLocker`。

实测 `Type=sqlserver`、拼错为 `redsi`、空字符串时，`ConfigureServices` 全部正常返回，却未注册 `IStaticCacheManager`、`ICacheKeyService` 和 `IDistributedCache`。解析 `ILocker` 随后报错：

```text
Unable to resolve service for type 'Microsoft.Extensions.Caching.Distributed.IDistributedCache'
while attempting to activate 'Girvs.Cache.Caching.DistributedCacheLocker'.
```

影响：配置错误表现为延后的 DI 异常；启用服务验证时也可能在容器构建阶段失败，而错误缺乏具体资源类型上下文。

建议：为不支持的类型抛出含资源键和类型的 `GirvsException`；若需恢复 SQL Server 支持，应作为完整功能处理，不能仅保留配置属性。

任务：[02-unsupported-resource-type.md](issues/02-unsupported-resource-type.md)。

## 3. P2：自行创建内存缓存，覆盖宿主配置

位置：`Girvs.Cache/GirvsCacheModule.cs:74`、`:94`、`:95`。

两处均直接 `new MemoryCache(new MemoryCacheOptions())` 并追加注册。宿主已有的 `IMemoryCache` 注册会被单服务解析覆盖，已有 `IOptions<MemoryCacheOptions>` 也没有被使用。

实测：先调用 `AddMemoryCache(o => o.SizeLimit = 1)`，再注册模块，未设置条目大小的写入仍成功，说明限制已失效。标准 `AddMemoryCache` 对照组同样的写入会抛出 `InvalidOperationException`。

影响：宿主设置的大小限制及其他缓存选项被忽略；自定义 `IMemoryCache` 实现也可能被替换。

建议：评估使用 `services.AddMemoryCache()` 保留已有注册并消费 Options；若模块需要专属缓存，应显式定义独立服务与其配置，不应隐式替换宿主缓存。

任务：[03-memory-options-overridden.md](issues/03-memory-options-overridden.md)。

## 4. P3：模块创建的内存缓存未交由 DI 管理释放

位置同第 3 项。

`AddSingleton<IMemoryCache>(instance)` 使用实例注册；容器不会负责释放该外部实例。模块创建了实例，却没有其他所有者执行 `Dispose`。

实测：解析缓存后释放根 `ServiceProvider`，该缓存仍能读写；标准 `AddMemoryCache` 对照组在容器释放后访问会抛出 `ObjectDisposedException`。

影响主要在同一进程重复启动、停止 Host 或反复创建容器的环境：缓存条目和资源不会随容器及时释放。不能据此断言长期运行的单 Host 必然发生持续增长的内存泄漏。

建议：使用容器创建的注册方式；标准 `AddMemoryCache()` 可一并解决配置及释放所有权问题。

任务：[04-memory-cache-disposal.md](issues/04-memory-cache-disposal.md)。

## 验证与边界

已有 `CacheModuleConnectionRefTests` 在 `net10.0`、`net11.0` 均为 2/2 通过。这两项只检查 `ServiceDescriptor`，不会实际获取 Redis 连接，因此无法捕获自锁。

```powershell
dotnet test tests\Girvs.Aspire.Hosting.Tests\Girvs.Aspire.Hosting.Tests.csproj -f net11.0 --filter FullyQualifiedName~CacheModuleConnectionRefTests --no-restore --verbosity minimal
dotnet test tests\Girvs.Aspire.Hosting.Tests\Girvs.Aspire.Hosting.Tests.csproj -f net10.0 --filter FullyQualifiedName~CacheModuleConnectionRefTests --no-restore --verbosity minimal
```

独立诊断程序位于 [probe/Program.cs](probe/Program.cs)，从仓库根目录运行：

```powershell
dotnet run --project .scratch\cache-module-investigation\probe\Probe.csproj -f net11.0 --no-restore --verbosity quiet
dotnet run --project .scratch\cache-module-investigation\probe\Probe.csproj -f net10.0 --no-restore --verbosity quiet
```

初次探查时退出码均为 1，表示复现了未修复问题。原始输出见 [net10-output.txt](net10-output.txt) 与 [net11-output.txt](net11-output.txt)。修复后重跑的输出见 [net11-fixed-output.txt](net11-fixed-output.txt)：三项自锁断言均已通过，其余三项遗留问题对应的 5 条断言仍失败。探针只验证连接流程能否完成或返回异常，不验证真实 Redis 读写、集群、重连成功等行为。同步自锁探针占用的后台线程随独立进程退出，不应移入正式测试进程后直接照用。

若在全新环境运行，应先执行 `dotnet restore .scratch\cache-module-investigation\probe\Probe.csproj`。本次实际使用本地 NuGet 包目录离线还原，未访问真实 Redis。

正向对照还验证了：默认内存缓存可读写；启用分布式缓存但未配置引用时按现有测试约定回退内存；Redis 类型匹配不区分大小写；缺少 Endpoints 时立即抛出 `GirvsException`。这些行为未作为缺陷报告。

初次探查没有修改生产实现和既有测试文件；后续最小修复改动及验证范围见修复验证文档。尚未执行真实 Redis 集成测试或完整解决方案测试。
