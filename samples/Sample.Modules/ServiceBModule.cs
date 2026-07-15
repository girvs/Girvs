using Girvs;
using Girvs.EntityFrameworkCore;
using Girvs.EventBus;

namespace Sample.Modules;

// ServiceB 使用 EventBus（CAP）；CAP 存储需一个库，故同时声明 EFCore 以让 Aspire
// 建 MySQL 库并注入连接，CAP 通过 DbConnectionString="default" 复用该连接作为存储。
[DependsOn(typeof(EventBusModule), typeof(GirvsEntityFrameworkCoreModule))]
public class ServiceBModule;
