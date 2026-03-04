# Girvs 开发框架 Agent 开发指南

## 项目概述

- **框架类型**: 基于 .NET 8.0/9.0/10.0 的企业级快速开发框架
- **解决方案**: `Girvs.slnx` (Solution Explorer 格式)
- **模块数量**: 15+ 个独立模块
- **目标框架**: `net8.0;net9.0;net10.0`

## 构建命令

```bash
# 构建整个解决方案
dotnet build Girvs.slnx

# 构建所有模块 (Release)
dotnet build Girvs.slnx -c Release

# 构建核心模块
dotnet build Girvs/Girvs.csproj

# 构建特定模块
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj

# 清理和重建
dotnet clean Girvs.slnx
dotnet build Girvs.slnx --no-incremental
```

## 测试命令

**注意**: 当前项目未包含测试项目。

```bash
# 创建测试项目
dotnet new xunit -n Girvs.Tests -o tests/Girvs.Tests
dotnet add tests/Girvs.Tests reference Girvs/Girvs.csproj

# 运行所有测试
dotnet test

# 运行单个测试
dotnet test --filter "FullyQualifiedName~YourTestClassName.YourTestMethodName"
```

## 代码风格指南

### 命名约定

- **类/接口/枚举**: `PascalCase`，接口必须以 `I` 开头
- **方法/属性**: `PascalCase`
- **私有字段**: `_camelCase` (推荐)
- **局部变量/参数**: `camelCase`

### 导入规范

- 使用 `GlobalUsings.cs` 定义全局 using 语句
- 项目特定的 using 放在文件顶部，按字母顺序排列

### 类型使用

- 优先使用接口而非具体实现进行依赖注入
- 使用泛型 `IEnumerable<T>` 作为集合类型
- 使用 `Task` 异步编程，避免 `async void`

### 错误处理

- 使用 `GirvsException` 抛出业务异常
- 带参数的异常消息: `throw new GirvsException("产品 {0} 不存在", statusCode: 404, error: null, productId);`

### 日志记录

使用 `ILogger<T>`，选择适当级别:
```csharp
_logger.LogInformation("获取产品 {ProductId} 成功", id);
_logger.LogWarning("产品 {ProductId} 库存不足", id);
_logger.LogError(ex, "产品 {ProductId} 查询失败", id);
```

### 实体定义

```csharp
public class Product : AggregateRoot<Guid>,
    IIncludeCreatorId<Guid>,
    IIncludeCreatorName,
    IIncludeMultiTenant<Guid>,
    IIncludeCreateTime,
    ITenantShardingTable,
    IIncludeMultiTenantName
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### DDD 模式 (Girvs.Driven)

- 使用 MediatR 处理 Command/Query
- 使用 FluentValidation 进行输入验证

```csharp
// Command
public class CreateOrderCommand : IRequest<Order>
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

// Handler
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Order>
{
    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken) { }
}
```

### 依赖注入

使用构造函数注入:
```csharp
services.AddScoped<IOrderService, OrderService>();
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddTransient<IEmailService, EmailService>();
```

## 注意事项

1. **ImplicitUsings**: 启用状态，全局 using 在 `GlobalUsings.cs` 中定义
2. **多目标框架**: 所有模块同时支持 net8.0/9.0/10.0，添加包引用时需注意版本条件
3. **版本号**: 所有模块保持一致 (当前 9.0.8.1)
4. **许可证**: Apache License 2.0

## 相关链接

- GitHub: https://github.com/girvs/Girvs
- NuGet: https://www.nuget.org/packages?q=Girvs