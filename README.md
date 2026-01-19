# Girvs 快速开发框架

<div align="center">

[![GitHub](https://img.shields.io/github/license/girvs/Girvs)](https://github.com/girvs/Girvs/blob/master/LICENSE)
[![Version](https://img.shields.io/badge/version-9.0.8-blue.svg)](https://github.com/girvs/Girvs)
[![.NET](https://img.shields.io/badge/.NET-8.0%20|%209.0%20|%2010.0-purple.svg)](https://dotnet.microsoft.com/)

</div>

## 📖 项目简介

Girvs 是一个基于 .NET 的企业级快速开发框架,旨在提供一套完整的微服务开发解决方��。框架采用模块化设计,集成了常用的基础设施组件,帮助开发者快速构建高性能、可扩展的分布式应用。

## ✨ 核心特性

- 🎯 **模块化架构** - 松耦合的模块设计,按需引入
- 🚀 **多版本支持** - 同时支持 .NET 8.0、9.0 和 10.0
- 🔧 **开箱即用** - 集成常用组件,减少重复开发
- 📦 **NuGet 支持** - 所有模块均可通过 NuGet 包管理
- 🔐 **安全可靠** - 内置权限认证、数据验证等安全机制
- 🌐 **微服务友好** - 支持服务注册发现、分布式事件总线
- 📊 **可观测性** - 完善的日志、监控支持

## 🏗️ 项目结构

```
Girvs/
├── Girvs                          # 核心框架模块
├── Girvs.AuthorizePermission      # 权限认证模块
├── Girvs.AutoMapper               # 对象映射模块
├── Girvs.Cache                    # 缓存模块 (支持 Redis/SqlServer)
├── Girvs.CodeGenerator            # 代码生成器模块
├── Girvs.Consul                   # 服务注册与发现模块
├── Girvs.Driven                   # 领域驱动设计支持模块
├── Girvs.DynamicWebApi            # 动态 Web API 模块
├── Girvs.EntityFrameworkCore      # EF Core 数据访问模块
├── Girvs.EventBus                 # 事件总线模块 (基于 CAP)
├── Girvs.Grpc                     # gRPC 服务模块
├── Girvs.OpenApi                  # OpenAPI 文档模块
├── Girvs.Quartz                   # 定时任务模块
├── Girvs.Refit                    # HTTP 客户端模块
└── Girvs.SignalR                  # 实时通信模块
```

## 📦 核心模块说明

### Girvs (核心框架)
框架的核心基础库,提供基本的抽象接口和工具类。

**主要功能:**
- 日志记录 (基于 Serilog)
- 配置管理
- 依赖注入扩展
- 通用工具类

**日志支持:**
- Console 输出
- 文件存储
- Elasticsearch 集成
- 阿里云日志服务

### Girvs.EntityFrameworkCore (数据访问层)
基于 Entity Framework Core 的数据访问模块。

**支持的数据库:**
- SQL Server
- MySQL (使用 Pomelo)
- SQLite
- Oracle
- InMemory (用于测试)

**主要功能:**
- Repository 模式
- Unit of Work 工作单元
- 数据库迁移管理
- 延迟加载支持 (Proxies)
- 表结构管理

### Girvs.Cache (缓存模块)
提供多种缓存实现的统一接口。

**支持的缓存方式:**
- Redis (基于 StackExchange.Redis)
- SQL Server
- 内存缓存

**主要功能:**
- 统一的缓存接口
- 缓存键管理
- 过期策略配置
- 异步操作支持

### Girvs.EventBus (事件总线)
基于 DotNetCore.CAP 的分布式事件总线实现。

**支持的消息队列:**
- RabbitMQ
- Kafka
- Redis Streams

**支持的存储:**
- MySQL
- SQL Server
- SQLite

**主要功能:**
- 发布/订阅模式
- 事件持久化
- 失败重试机制
- 可视化管理界面 (Dashboard)

### Girvs.DynamicWebApi (动态 API)
自动将应用服务转换为 RESTful API。

**主要功能:**
- 自动路由生成
- RESTful 风格 API
- 模型状态验证
- Mini API 支持

### Girvs.AuthorizePermission (权限认证)
完整的权限认证解决方案。

**主要功能:**
- 自定义认证方案
- 基于角色的权限控制
- 数据规则过滤
- 方法级权限控制
- 权限缓存管理

### Girvs.Grpc (gRPC 服务)
gRPC 服务支持模块。

**主要功能:**
- gRPC 服务配置
- 异常拦截器
- 自动服务注册

### Girvs.Quartz (定时任务)
基于 Quartz.NET 的定时任务调度。

**主要功能:**
- 作业调度
- Cron 表达式支持
- 单例作业工厂
- 后台任务托管

### Girvs.Consul (服务注册发现)
基于 Consul 的服务注册与发现。

**主要功能:**
- 服务注册
- 健康检查
- 服务发现
- 配置中心

### Girvs.SignalR (实时通信)
基于 SignalR 的实时通信模块。

**主要功能:**
- WebSocket 支持
- JWT 认证集成
- 端点路由配置

### Girvs.Refit (HTTP 客户端)
声明式 HTTP 客户端封装。

**主要功能:**
- Refit 集成
- 自动服务注册
- 自定义 HttpClient 处理器

### Girvs.AutoMapper (对象映射)
基于 AutoMapper 的对象映射模块。

**主要功能:**
- 自动配置发现
- 有序映射配置
- 依赖注入集成

### Girvs.Driven (领域驱动)
DDD (领域驱动设计) 支持模块。

**主要功能:**
- 命令 (Command)
- 查询 (Query)
- 事件 (Event)
- 通知 (Notification)
- MediatR 集成
- 验证行为
- 缓存行为

### Girvs.OpenApi (API 文档)
OpenAPI 3.0 文档生成。

**主要功能:**
- Swagger UI 集成
- Bearer Token 认证
- 自定义参数过滤
- 自动绑定约定

### Girvs.CodeGenerator (代码生成器)
代码脚手架工具。

**主要功能:**
- 代码模板管理
- 代码生成服务
- 自定义模板支持

## 🚀 快速开始

### 环境要求

- .NET SDK 8.0 或更高版本
- Visual Studio 2022 或 JetBrains Rider
- (可选) Docker Desktop

### 安装

通过 NuGet 包管理器安装所需模块:

```bash
# 安装核心框架
dotnet add package Girvs --version 9.0.8

# 安装 EntityFrameworkCore 模块
dotnet add package Girvs.EntityFrameworkCore --version 9.0.8

# 安装缓存模块
dotnet add package Girvs.Cache --version 9.0.8

# 安装事件总线
dotnet add package Girvs.EventBus --version 9.0.8
```

### 基础使用示例

#### 1. 配置 Startup

```csharp
public class Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    : IGirvsStartup
{
    public IConfiguration Configuration { get; } = configuration;
    public IWebHostEnvironment WebHostEnvironment { get; } = webHostEnvironment;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithAuthorizePermissionFilter(options =>
            options.Filters.Add<GirvsModelStateInvalidFilter>()
        );
        services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseGirvsExceptionHandler();
        app.UseRouting();
        app.ConfigureRequestPipeline(env);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.ConfigureEndpointRouteBuilder();
        });
    }
}
```

#### 2. 数据访问示例

```csharp
// 定义实体
public class Product : AggregateRoot<Guid>, 
    IIncludeCreatorId<Guid>, 
    IIncludeCreatorName, 
    IIncludeMultiTenant<Guid>, 
    IIncludeCreateTime, 
    ITenantShardingTable, //按租户自动分表 
    IIncludeMultiTenantName // 多租户字段
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// 使用仓储
public class ProductService
{
    private readonly IRepository<Product> _productRepository;

    public ProductService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product> GetByIdAsync(Guid id)
    {
        return await _productRepository.GetByIdAsync(id);
    }
}
```

#### 3. 事件总线示例

```csharp
// 定义事件
public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

// 发布事件
public class OrderService
{
    private readonly IEventBus _eventBus;

    public async Task CreateOrderAsync(Order order)
    {
        // 保存订单...

        // 发布事件
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            Amount = order.Amount
        });
    }
}

// 订阅事件
public class OrderEventHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // 处理订单创建事件
        Console.WriteLine($"订单已创建: {@event.OrderId}");
    }
}
```

## 📝 配置说明


## 🔧 版本支持

| 模块 | .NET 8.0 | .NET 9.0 | .NET 10.0 |
|------|----------|----------|-----------|
| 所有模块 | ✅ | ✅ | ✅ |

当前版本: **9.0.8**

## 📚 学习资源

### 借鉴的优秀开源项目

1. **nopCommerce** - https://github.com/nopSolutions/nopCommerce
   - 电商领域的最佳实践
   - 插件化架构设计

2. **ChristDDD** - https://github.com/anjoy8/ChristDDD
   - DDD 领域驱动设计实现
   - CQRS 模式应用

3. **eShopOnContainers** - https://github.com/dotnet-architecture/eShopOnContainers
   - 微服务架构参考实现
   - 容器化部署方案

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request!

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交 Pull Request

## 📄 许可证

本项目采用 [Apache License 2.0](https://github.com/girvs/Girvs/blob/master/LICENSE) 许可证。

## 🔗 相关链接

- **GitHub 仓库**: https://github.com/girvs/Girvs
- **问题反馈**: https://github.com/girvs/Girvs/issues
- **NuGet 包**: https://www.nuget.org/packages?q=Girvs

## 👨‍💻 作者

**kicck**

## 📮 联系方式

如有问题或建议,欢迎通过 GitHub Issues 与我们联系。

---

<div align="center">

**⭐ 如果这个项目对你有帮助,请给个星标支持一下! ⭐**

</div>
