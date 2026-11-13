# 05 — 依赖容器与静态状态

Status: completed
Blocked by: 04

## 完成内容

- `Singleton<T>` 读写加锁，避免并发写入丢失；写入 `null` 时从注册表移除，兼容原先"空值即未注册"的语义。
- `BaseSingleton.AllSingletons` 由 `Dictionary` 改为 `ConcurrentDictionary`，并删除不必要的静态构造。
- `EngineContext.Create` 移除 `[MethodImpl(MethodImplOptions.Synchronized)]`，改为 `lock` + `??=`，只创建一次引擎。
- `GirvsEngine.ConfigureServices` 不再调用 `services.BuildServiceProvider()`，消除重复容器与由此产生的单例分叉。
- 新增 `GirvsEngine.SetServiceProvider`，由 `GirvsHostBuilderManager.CreateGirvsWebApplication` 在 `builder.Build()` 后回填真实 `IServiceProvider`。
- 启动顺序调整为：`builder.Build()` → 回填服务提供程序 → 各 `IGirvsStartup.Configure` → 请求管道 → 端点映射。

## 行为变化

- 在 `Startup.ConfigureServices` 阶段调用 `EngineContext.Current.Resolve<T>()` 将返回 `null`，因为容器尚未构建。既有模块（如 `Girvs.Consul/ConsulModule.cs:41`）均在 `Configure` 阶段解析，回填后不受影响。
- `Singleton<T>.Instance = null` 现在等价于"移除注册"，`AllSingletons` 中不再保留空值项。

## 测试

新增 `EngineContextTests`（4 项）：

- `Create` 多次调用返回同一引擎实例。
- 并发写入 `Singleton<string>` 不抛异常。
- 写入空值后从 `BaseSingleton.AllSingletons` 移除。
- `EngineContext.Replace` 后 `Current` 返回替换实例。

该测试类使用 `[CollectionDefinition(DisableParallelization = true)]` 隔离，避免与其它依赖静态状态的测试互相污染。

## 验证结果

- `Girvs.Core.Tests` 四目标各 58 项通过。
- `Girvs.Claims.Tests` net10 46 项通过。
- 全仓构建 0 错误。
