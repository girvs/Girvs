using Girvs;
using Girvs.Cache;
using Girvs.EntityFrameworkCore;

namespace Sample.Modules;

// 声明 ServiceA 使用的 Girvs 组件：缓存 + EFCore 数据访问
[DependsOn(typeof(GirvsCacheModule), typeof(GirvsEntityFrameworkCoreModule))]
public class ServiceAModule;
