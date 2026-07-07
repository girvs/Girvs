\# C# .NET 代码走查详细检查项



\## 目录

1\. \[代码质量](#一代码质量)

2\. \[安全审查](#二安全审查)

3\. \[性能优化](#三性能优化)

4\. \[架构合规](#四架构合规)



---



\## 一、代码质量



\### 1.1 命名规范

| 检查项 | 级别 | 说明 |

|--------|------|------|

| 类名 PascalCase | 🔵 规范 | `UserService` ✅ `userService` ❌ |

| 方法名 PascalCase | 🔵 规范 | `GetUser()` ✅ `getUser()` ❌ |

| 私有字段 `\_camelCase` | 🔵 规范 | `\_userId` ✅ `userId` ❌ |

| 接口以 `I` 开头 | 🔵 规范 | `IUserService` ✅ `UserService` ❌ |

| 异步方法以 `Async` 结尾 | 🔵 规范 | `GetUserAsync()` ✅ `GetUser()` ❌ |

| 禁止魔法数字/字符串 | 🟡 建议 | 用常量或枚举替代 `if (status == 2)` |

| 避免无意义命名 | 🟠 中危 | `Manager2`、`Helper3`、`Temp` |



\### 1.2 代码结构

| 检查项 | 级别 | 说明 |

|--------|------|------|

| 单方法不超过 50 行 | 🟡 建议 | 超过需拆分，降低圈复杂度 |

| 单类不超过 500 行 | 🟡 建议 | 过大考虑职责拆分 |

| 嵌套层级不超过 4 层 | 🟠 中危 | 使用 early return / Guard Clause |

| 无注释掉的死代码 | 🟡 建议 | 删除而非注释保留 |

| TODO 有 Issue 跟踪 | 🟡 建议 | 避免无主 TODO 积累 |



\*\*Early Return 示例：\*\*

```csharp

// ❌ 深嵌套

public Result Process(Order order) {

&nbsp;   if (order != null) {

&nbsp;       if (order.Items.Any()) {

&nbsp;           if (order.IsValid()) {

&nbsp;               // 业务逻辑...

&nbsp;           }

&nbsp;       }

&nbsp;   }

}



// ✅ Early Return

public Result Process(Order order) {

&nbsp;   if (order == null) return Result.Fail("订单不存在");

&nbsp;   if (!order.Items.Any()) return Result.Fail("订单无商品");

&nbsp;   if (!order.IsValid()) return Result.Fail("订单无效");

&nbsp;   // 业务逻辑...

}

```



\### 1.3 SOLID 原则

| 检查项 | 级别 | 说明 |

|--------|------|------|

| Controller 不含业务逻辑 | 🟠 中危 | 业务逻辑应在 Service 层 |

| 类职责单一 | 🟡 建议 | 一个类只做一件事 |

| 依赖抽象（接口）而非实现 | 🟠 中危 | 构造函数注入 `IUserService` 而非 `UserService` |

| 不直接 `new` 有依赖的服务 | 🟠 中危 | 应通过 DI 容器 |



\### 1.4 异常处理

| 检查项 | 级别 | 示例 |

|--------|------|------|

| 禁止空 catch | 🔴 高危 | `catch { }` 或 `catch(Exception e) { }` |

| 不用异常控制流程 | 🟠 中危 | try/catch 不能代替 if/else |

| 全局异常中间件 | 🔴 高危 | `UseExceptionHandler` 或自定义 Middleware |

| 正确释放资源 | 🔴 高危 | `using` 或 `IDisposable` |

| 自定义异常有 HTTP 映射 | 🟠 中危 | `NotFoundException` → 404 |



```csharp

// ❌ 空catch - 高危

try { await \_db.SaveChangesAsync(); }

catch (Exception) { }



// ✅ 正确处理

try { await \_db.SaveChangesAsync(); }

catch (DbUpdateException ex) {

&nbsp;   \_logger.LogError(ex, "保存失败，OrderId: {OrderId}", order.Id);

&nbsp;   throw new BusinessException("保存订单失败");

}

```



\### 1.5 日志规范

| 检查项 | 级别 |

|--------|------|

| 禁止 `Console.WriteLine` 在生产代码 | 🟠 中危 |

| 使用结构化日志（Serilog/NLog） | 🟠 中危 |

| 日志中不含密码/Token/身份证 | 🔴 高危 |

| 关键业务操作有审计日志 | 🟠 中危 |

| 日志级别使用正确 | 🟡 建议 |



---



\## 二、安全审查



\### 2.1 输入验证与注入防护

| 检查项 | 级别 |

|--------|------|

| 所有外部输入有验证（FluentValidation / DataAnnotations） | 🔴 高危 |

| EF Core 参数化查询，禁止拼接 SQL | 🔴 高危 |

| 输出 HTML 编码（防 XSS） | 🔴 高危 |

| 文件上传双重校验（MIME + 扩展名） | 🔴 高危 |

| 禁止不可信数据 BinaryFormatter 反序列化 | 🔴 高危 |



```csharp

// ❌ SQL 注入风险

var sql = $"SELECT \* FROM Users WHERE Name = '{name}'";

var users = \_db.Users.FromSqlRaw(sql).ToList();



// ✅ 参数化查询

var users = \_db.Users

&nbsp;   .Where(u => u.Name == name)

&nbsp;   .ToList();

// 或

var users = \_db.Users

&nbsp;   .FromSqlRaw("SELECT \* FROM Users WHERE Name = {0}", name)

&nbsp;   .ToList();

```



\### 2.2 认证与授权

| 检查项 | 级别 |

|--------|------|

| JWT Access Token 过期时间 ≤ 1 小时 | 🔴 高危 |

| 所有敏感接口标注 `\[Authorize]` | 🔴 高危 |

| 数据行级别权限隔离（防越权） | 🔴 高危 |

| 密码使用 BCrypt/Argon2 存储 | 🔴 高危 |

| 禁止 MD5/SHA1 存储密码 | 🔴 高危 |

| 敏感操作有 CSRF 防护 | 🟠 中危 |



```csharp

// ❌ 越权漏洞

\[HttpGet("{orderId}")]

public async Task<Order> GetOrder(int orderId) {

&nbsp;   return await \_db.Orders.FindAsync(orderId); // 未验证订单归属！

}



// ✅ 数据隔离

\[HttpGet("{orderId}")]

public async Task<Order> GetOrder(int orderId) {

&nbsp;   var userId = \_currentUser.GetUserId();

&nbsp;   var order = await \_db.Orders

&nbsp;       .Where(o => o.Id == orderId \&\& o.UserId == userId) // 加用户过滤

&nbsp;       .FirstOrDefaultAsync();

&nbsp;   if (order == null) throw new NotFoundException();

&nbsp;   return order;

}

```



\### 2.3 敏感信息管理

| 检查项 | 级别 |

|--------|------|

| appsettings.json 无硬编码密码/Key | 🔴 高危 |

| 生产密钥通过 KeyVault / 环境变量注入 | 🔴 高危 |

| .gitignore 包含 appsettings.Production.json | 🔴 高危 |

| 数据库连接使用最小权限账号 | 🟠 中危 |



\### 2.4 传输与接口安全

| 检查项 | 级别 |

|--------|------|

| 强制 HTTPS | 🔴 高危 |

| API 响应不返回多余字段（使用 DTO） | 🟠 中危 |

| CORS 配置精确 AllowedOrigins | 🟠 中危 |

| 生产环境 Swagger 有鉴权保护 | 🟠 中危 |

| 接口有限流（Rate Limiting） | 🟠 中危 |



---



\## 三、性能优化



\### 3.1 EF Core 优化

| 检查项 | 级别 |

|--------|------|

| 只读查询使用 `AsNoTracking()` | 🟠 中危 |

| 无 N+1 查询（使用 Include/Join） | 🔴 高危 |

| 大数据量分页（禁止全表 ToList） | 🔴 高危 |

| Select 投影只取需要的字段 | 🟡 建议 |

| 批量操作使用 `ExecuteUpdateAsync`/`ExecuteDeleteAsync` | 🟡 建议 |



```csharp

// ❌ N+1 查询

var orders = await \_db.Orders.ToListAsync();

foreach (var order in orders) {

&nbsp;   var items = await \_db.OrderItems

&nbsp;       .Where(i => i.OrderId == order.Id).ToListAsync(); // 每条订单多一次查询！

}



// ✅ 一次 Include 加载

var orders = await \_db.Orders

&nbsp;   .Include(o => o.Items)

&nbsp;   .AsNoTracking()

&nbsp;   .ToListAsync();

```



\### 3.2 async/await 规范

| 检查项 | 级别 |

|--------|------|

| I/O 操作全部 async/await | 🔴 高危 |

| 禁止 `.Result` / `.Wait()` 阻塞 | 🔴 高危 |

| 禁止 `async void`（除事件处理器） | 🟠 中危 |

| CancellationToken 透传 | 🟡 建议 |



```csharp

// ❌ 阻塞调用 - 高危（可能导致死锁）

public User GetUser(int id) {

&nbsp;   return \_userService.GetUserAsync(id).Result;

}



// ✅ 正确异步

public async Task<User> GetUserAsync(int id) {

&nbsp;   return await \_userService.GetUserAsync(id);

}

```



\### 3.3 缓存使用

| 检查项 | 级别 |

|--------|------|

| 热点数据接入缓存（Redis/MemoryCache） | 🟠 中危 |

| 缓存 Key 规范，有命名前缀 | 🟡 建议 |

| 缓存有合理过期时间 | 🟠 中危 |

| 缓存击穿/穿透/雪崩防护 | 🟠 中危 |



\### 3.4 并发与内存

| 检查项 | 级别 |

|--------|------|

| 并发场景使用线程安全集合 | 🟠 中危 |

| 避免大对象（>85KB）频繁分配 | 🟡 建议 |

| HttpClient 通过工厂注入，禁止手动 `new HttpClient()` | 🟠 中危 |



---



\## 四、架构合规



\### 4.1 分层规范

| 检查项 | 级别 |

|--------|------|

| Controller → Service → Repository 分层清晰 | 🟠 中危 |

| Controller 只做路由分发和参数绑定 | 🟠 中危 |

| Repository 只做数据访问，无业务逻辑 | 🟠 中危 |

| DTO / Domain Model / Entity 三者分离 | 🟠 中危 |

| 禁止直接暴露 EF 实体到 API 响应 | 🟠 中危 |



\### 4.2 依赖注入

| 检查项 | 级别 |

|--------|------|

| 所有服务通过构造函数注入 | 🟠 中危 |

| Scoped/Singleton/Transient 生命周期使用正确 | 🟠 中危 |

| 禁止在 Singleton 中注入 Scoped 服务 | 🔴 高危 |



```csharp

// ❌ Singleton 注入 Scoped - 高危（内存泄漏 + 数据错乱）

public class MySingletonService {

&nbsp;   private readonly IUserRepository \_repo; // Scoped 服务！

&nbsp;   public MySingletonService(IUserRepository repo) { \_repo = repo; }

}



// ✅ 通过 IServiceScopeFactory 解决

public class MySingletonService {

&nbsp;   private readonly IServiceScopeFactory \_scopeFactory;

&nbsp;   public MySingletonService(IServiceScopeFactory factory) { \_scopeFactory = factory; }

&nbsp;   public async Task DoWork() {

&nbsp;       using var scope = \_scopeFactory.CreateScope();

&nbsp;       var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

&nbsp;       // ...

&nbsp;   }

}

```



\### 4.3 可维护性

| 检查项 | 级别 |

|--------|------|

| 配置通过 `IOptions<T>` 强类型绑定 | 🟡 建议 |

| 枚举代替魔法数字，有注释说明 | 🟡 建议 |

| Migration 文件完整，可一键迁移 | 🟠 中危 |

| API 有 Swagger 文档 | 🟡 建议 |

| 架构文档描述模块职责和部署方式 | 🟡 建议 |

