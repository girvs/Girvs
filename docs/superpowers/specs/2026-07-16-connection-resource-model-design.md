# 连接资源配置模型设计

## 目标

连接资源由 Girvs 核心统一保存，具体模块自行解释其配置。该模型同时支持单体应用和 Aspire：单体从本地 `appsettings.json` 绑定；Aspire 先合并 AppHost 的共享配置与本地配置，再绑定为同一份 `AppSettings`。

## 核心模型

`AppSettings` 只提供资源字典：

```csharp
public Dictionary<string, Resource> Resources { get; set; } = new();

public class Resource
{
    public string Type { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

资源名称是稳定键，例如 `primary-mysql`、`platform-redis`；`Type` 是可扩展字符串，不是枚举。Girvs 核心不包含 MySQL、Redis、Kafka 或 Elasticsearch 的适配器。

```json
{
  "Resources": {
    "primary-mysql": {
      "Type": "mysql",
      "Settings": { "Host": "mysql", "Port": "3306", "UserName": "app" }
    },
    "platform-redis": {
      "Type": "redis",
      "Settings": { "Endpoints": "redis:6379", "Ssl": "false" }
    }
  }
}
```

`Settings` 的值是字符串；多个 Redis endpoint 使用逗号分隔，例如 `"redis-1:6379,redis-2:6379"`。

## 模块消费

模块配置中的 `ConnectionRef` 直接指向 `Resources` 的键。模块从 `Singleton<AppSettings>.Instance.Resources` 读取资源，检查所需的 `Type`，并将自己的业务参数与资源设置组合。例如，缓存模块解释 Redis endpoint，EntityFrameworkCore 解释 MySQL 主机和认证信息，EventBus 分别解释持久化数据库与 Redis Transport。

资源不存在或类型不匹配时，模块抛出 `GirvsException`。未配置 `ConnectionRef` 时保持原有连接串配置，因此不影响既有单体应用。

## Cache 连接组装

`DistributedCacheConfig` 不保存 `ConnectionString`，而是保留模块专属的 `DefaultDatabase`、`InstanceName`、`SchemaName` 和 `TableName`。当缓存类型为 `SqlServer` 时，`ConnectionRef` 必须引用 `Type: "sqlserver"` 的资源；当类型为 `Redis` 或 `RedisSynchronizedMemory` 时，必须引用 `Type: "redis"` 的资源。`GirvsCacheModule` 在注册缓存服务前读取该资源并组装实际连接串：Redis 使用 `Endpoints`、可选 `Ssl` 与模块的 `DefaultDatabase`；SqlServer 使用资源中的连接信息。Aspire 注入的 `girvs-cache` 连接串仍以最高优先级覆盖组装结果。
