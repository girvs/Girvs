# 连接资源与模块配置复用设计

## 背景与目标

Girvs.Cache、Girvs.EventBus 与 Girvs.EntityFrameworkCore 都需要数据库或 Redis 连接。现有 EventBus 通过字符串匹配 `DbConfig` 名称、通过配置节名称读取 Cache 配置；这种隐式约定无法表达 Redis `defaultDatabase`、MySQL `database` 等使用参数，也无法供 Aspire Hosting 稳定复用资源。

本设计建立显式的“物理资源 + 连接绑定 + 模块引用”模型，并同时支持单体应用与 Aspire 微服务。服务本地配置优先于 AppHost 提供的共享默认配置。

## 设计决策

在 `Girvs/` 定义不依赖 Aspire 的配置契约、适配器注册表与解析器：

- `Resources`：按资源名扁平声明 MySQL Server、Redis Server、Kafka、Elasticsearch 等物理资源。
- `Bindings`：将物理资源绑定为可消费的数据库、Redis 逻辑库或其他使用配置。
- `ConnectionRef`：模块配置对 Binding 的显式引用。
- `IConnectionResourceResolver`：把 Binding 解析为最终连接信息。
- `IInfrastructureResourceAdapter`：按资源类型解析资源专属的 `Settings` 与 Binding 的 `Options`。

`Girvs.Aspire.Hosting/` 只负责读取共享配置、与服务配置深度合并、编排 Aspire 资源以及注入有效配置。`Girvs.Cache`、`Girvs.EventBus` 和 `Girvs.EntityFrameworkCore` 只通过解析器消费连接信息，不依赖彼此，也不依赖 Aspire。

## 配置模型

```json
{
  "Resources": {
    "primary-mysql": {
      "Type": "mysql",
      "Settings": {
        "Host": "mysql",
        "Port": 3306,
        "UserName": "app",
        "PasswordRef": "mysql-password"
      }
    },
    "platform-redis": {
      "Type": "redis",
      "Settings": {
        "Endpoints": [ "redis:6379" ],
        "Ssl": false
      }
    }
  },
  "Bindings": {
    "AilynxDb": {
      "Resource": "primary-mysql",
      "Options": { "Database": "Wb_Ailynx_2022" }
    },
    "AilynxCache": {
      "Resource": "platform-redis",
      "Options": {
        "DefaultDatabase": 1,
        "InstanceName": "ailynx:"
      }
    }
  }
}
```

资源类型是可扩展、全小写的字符串标识符，而不是枚举。框架为 `redis`、`mysql`、`kafka`、`elasticsearch` 等内置类型提供常量与适配器；模块或业务方可注册自定义类型适配器，例如 `aliyun-rocketmq`。资源的 `Settings` 不固定为 `ConnectionString`，由类型适配器解释：Redis 使用 `Endpoints`，Kafka 使用 `BootstrapServers`，Elasticsearch 使用 `NodeUris`。Binding 的 `Options` 只表达使用差异。

MySQL Binding 以物理 Server 设置为基准设置 `Database`；Redis Binding 在物理 Redis 设置上追加 `DefaultDatabase`，并将 `InstanceName` 作为使用方配置返回。多个 Binding 可以引用同一物理资源，但使用不同数据库名、逻辑库或前缀。

模块配置以 Binding 名引用：EF Core 的命名数据连接、Cache 的分布式缓存连接、EventBus 的持久化数据库连接与 Redis Transport 连接分别引用 Binding。EventBus 可引用与 EF Core 相同的数据库 Binding，也可引用与 Cache 相同或不同的 Redis Binding。

## 单体与 Aspire 运行模式

单体应用把 `Resources`、`Bindings` 和模块配置放入本地 `appsettings.json`，由 Girvs 核心解析器直接解析；不需要引用 Aspire 包。

Aspire 模式下，AppHost 提供共享默认 `Resources` 与 `Bindings`。Hosting 读取 AppHost 共享配置和服务本地 `appsettings.json`，按配置路径深度合并，后者覆盖前者；再将合并后的有效配置注入服务。这样环境变量的高优先级不会反向覆盖本地配置，因为注入值已是合并结果。Hosting 依据 Binding 所引用的物理资源创建或复用 MySQL、Redis 等 Aspire 资源，并以同一 Binding 名注入服务。

## 兼容性、安全性与错误处理

不兼容旧的 `DbConnectionString: "Ailynx"`、`RedisConnectionString: "CacheConfig"` 隐式字符串查找。升级后模块必须使用明确的 `ConnectionRef`。

资源敏感设置不得进入仓库：单体使用环境变量或 Secret provider；Aspire 发布模式使用 Parameter/Secret。缺失 Binding、资源不存在、类型适配器未注册、资源类型不匹配、数据库名为空或 Redis 逻辑库非法时，解析器必须在启动阶段抛出带引用路径的 `GirvsException`，禁止静默回退。

## 验证范围

测试覆盖：单体直接解析；本地配置覆盖共享默认值；EF Core 与 EventBus 共用数据库 Binding；Cache 与 EventBus 共用或隔离 Redis 逻辑库；缺失或类型不匹配的 Binding；Aspire 开发与发布模式资源复用和 Secret 注入。示例项目使用占位凭据演示完整配置。
