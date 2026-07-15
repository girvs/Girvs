using Girvs;
using Girvs.Cache;

namespace Sample.Modules;

// 声明 ServiceA 使用的 Girvs 组件：缓存（后续增量再叠加 EFCore）
[DependsOn(typeof(GirvsCacheModule))]
public class ServiceAModule;
