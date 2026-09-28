# BuildConnectionString 追加 Allow User Variables 参数 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `DataConnectionConfig.BuildConnectionString` 生成的 MySQL 连接字符串末尾硬编码追加 `Allow User Variables=true`。

**Architecture:** 在 `DbConnectionStringBuilder` 上追加一个布尔键值对 `["Allow User Variables"] = true`，使 MySqlConnector 驱动的所有 MySQL 连接均允许 SQL 中使用用户变量（`@var`）。无新增配置项，无接口变更。

**Tech Stack:** .NET 8/9/10（多目标框架）、xUnit、MySqlConnector（Pomelo.EntityFrameworkCore.MySql / Microting.EntityFrameworkCore.MySql 底层驱动）。

## Global Constraints

- 硬编码追加，不引入配置开关（已与需求方确认）。
- 仅对 `resource.Type == "mysql"` 生效（方法内已有类型校验，非 mysql 直接抛异常）。
- 主库与读库连接串均经由 `BuildConnectionString` 生成，一并生效。
- 兼容 `net8.0;net9.0;net10.0` 三个目标框架。

---

### Task 1: 在 BuildConnectionString 末尾追加 Allow User Variables

**Files:**
- Modify: `Girvs.EntityFrameworkCore/Configuration/DataBaseConfig.cs:202-204`

**Interfaces:**
- Consumes: 无新增依赖。
- Produces: `BuildConnectionString` 返回的连接串末尾包含 `Allow User Variables=True`。

- [ ] **Step 1: 追加硬编码参数**

在 `builder["Password"] = password;` 分支之后、`return builder.ConnectionString;` 之前，添加一行：

```csharp
        if (
            resource.Settings.TryGetValue("Password", out var password)
            && !string.IsNullOrWhiteSpace(password)
        )
            builder["Password"] = password;

        builder["Allow User Variables"] = true;   // <-- 新增

        return builder.ConnectionString;
```

- [ ] **Step 2: 编译验证**

运行：

```bash
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj --nologo
```

预期：Build succeeded。

---

### Task 2: 更新现有集成测试，验证 Allow User Variables 已写入连接串

**Files:**
- Modify: `tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs:87-94`

**Interfaces:**
- Consumes: `DataConnectionConfig.BuildConnectionString`（Task 1 的产出）。
- Produces: 新增两条断言，验证主库和读库连接串均包含 `Allow User Variables=True`。

- [ ] **Step 1: 在现有测试 `EF主从库与EventBus持久化连接均从资源组装` 中追加断言**

在 `db.ResolveConnectionStrings(resources);` 之后的断言块中，已有：
```csharp
Assert.Contains("Server=mysql", db.GetMasterDataConnectionString());
Assert.Contains("Server=mysql-read", db.GetSecureRandomReadDataConnectionString());
```

紧接其后追加两行：

```csharp
Assert.Contains("Allow User Variables=True", db.GetMasterDataConnectionString());
Assert.Contains("Allow User Variables=True", db.GetSecureRandomReadDataConnectionString());
```

完整上下文（修改后的测试方法 `EF主从库与EventBus持久化连接均从资源组装` 中第 87-97 行区域）：

```csharp
        db.ResolveConnectionStrings(resources);

        Assert.Contains("Server=mysql", db.GetMasterDataConnectionString());
        Assert.Contains("Server=mysql-read", db.GetSecureRandomReadDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetMasterDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetSecureRandomReadDataConnectionString());
        Assert.Equal(
            db.GetMasterDataConnectionString(),
            eventBus.BuildPersistenceConnectionString(resources["ailynx"])
        );
```

- [ ] **Step 2: 运行新增断言验证失败（TDD 红阶段，如果尚未修改生产代码）**

若 Task 1 的生产代码尚未写入，运行：

```bash
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~EF主从库与EventBus持久化连接均从资源组装" --nologo
```

预期：FAIL — 断言找不到 `Allow User Variables=True`。

若 Task 1 已完成，跳至 Step 3。

- [ ] **Step 3: 运行全量测试验证通过**

```bash
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo
```

预期：全部 8 个测试 PASS。

- [ ] **Step 4: 回归验证其余测试项目**

```bash
dotnet test Girvs.slnx --nologo
```

预期：全部测试 PASS（无回归）。

---

### Task 3: 提交变更

**Files:**
- 已修改：`Girvs.EntityFrameworkCore/Configuration/DataBaseConfig.cs`
- 已修改：`tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs`

- [ ] **Step 1: 暂存并提交**

```bash
git add Girvs.EntityFrameworkCore/Configuration/DataBaseConfig.cs tests/Girvs.Aspire.Hosting.Tests/ModuleConnectionReferenceTests.cs
git commit -m "feat: BuildConnectionString 追加 Allow User Variables=true"
```

---

## 验证 Checklist

| 验收项 | 验证方式 | 预期结果 |
|---|---|---|
| 编译通过 | `dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj` | Build succeeded |
| `BuildConnectionString` 输出包含 `Allow User Variables=True` | 单元测试断言 | PASS |
| 主库连接串包含参数 | `GetMasterDataConnectionString()` 断言 | PASS |
| 读库连接串包含参数 | `GetSecureRandomReadDataConnectionString()` 断言 | PASS |
| 非 mysql 资源不受影响 | 现有异常路径测试 | `EF引用的资源不存在时立即抛出()` 等测试 PASS |
| 全量测试无回归 | `dotnet test Girvs.slnx` | 全部 PASS |
