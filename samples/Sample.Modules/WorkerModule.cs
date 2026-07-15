using Girvs;
using Girvs.Cache;

namespace Sample.Modules;

// 后台工作服务使用缓存组件
[DependsOn(typeof(GirvsCacheModule))]
public class WorkerModule;
