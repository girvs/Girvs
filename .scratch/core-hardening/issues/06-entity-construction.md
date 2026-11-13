# 06 — 实体构造副作用

Status: completed
Blocked by: 05

## 完成内容

- `BaseEntity<TPrimaryKey>` 新增 `ConcurrentDictionary<Type, EntityFormatMetadata>` 缓存，按实体类型缓存 `TenantId`、`CreatorId` 属性与是否需要身份上下文，避免每次实例化重复反射。
- 只有实体确实包含创建人、租户字段时才访问 `EngineContext.Current.PrincipalAccessor`，减少无字段实体对全局引擎的依赖。
- `TenantId`、`CreatorId` 赋值统一走 `TrySetConvertedValue`：属性不可写、值为空或转换失败时保留默认值，实体构造不再因身份字段格式异常而中断。
- `GirvsConvert` 新增 `ToSpecifiedType(Type, object)` 重载，支持可空类型、`string`、`Guid`、枚举；`ToSpecifiedType<T>` 改为按 `Type` 转换，修复 `Nullable<T>` 的 `FullName` 无法识别问题。
- `Equals`、`GetHashCode` 改用 `EqualityComparer<TPrimaryKey>.Default`，引用类型主键为 `null` 时不再抛 `NullReferenceException`。

## 未采纳

- 未引入 EF Core `SaveChangesInterceptor` 接管系统字段。当前系统字段由实体构造函数填充，EF 物化实体时也会执行；切换为拦截器会改变审计语义与写入时机，需单独评估后另行实施。

## 测试

- 新增 `BaseEntityTests`（5 项）：构造填充时间字段、无身份上下文时租户/创建人保持默认、引用类型主键为空时相等比较与哈希不抛、相同主键相等、不同主键不等。
- `GirvsConvertTests` 新增 4 项：按 `Type` 转换 `Guid?`、`string`、枚举、失败抛 `FormatException`。

## 验证结果

- `Girvs.Core.Tests` 四目标各 67 项通过。
- 全仓构建 0 错误，改动文件无新增警告。
