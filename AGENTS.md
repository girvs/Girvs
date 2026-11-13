# Repository Guidelines

## 项目结构与模块组织

`Girvs.slnx` 包含核心库 `Girvs/` 及各独立模块，如 `Girvs.Aspire/`、`Girvs.Aspire.Hosting/`、`Girvs.Aspire.Gateway/`、`Girvs.EntityFrameworkCore/` 和 `Girvs.Driven/`。示例应用位于 `samples/`；其中 `Sample.AppHost` 用于 Aspire 编排，`Sample.ServiceA`、`Sample.ServiceB` 和 `Sample.Worker` 用于端到端验证。单元测试位于 `tests/`，按被测模块分组。每个模块保持独立的 `.csproj` 与 `GlobalUsings.cs`。

## 构建、测试与本地开发

项目使用 .NET SDK 10.0.100（见 `global.json`），库默认多目标编译为 `net8.0;net9.0;net10.0`；Aspire 相关模块仅支持 `net10.0`。

```bash
dotnet build Girvs.slnx                 # 构建全部模块
dotnet build Girvs.slnx -c Release      # 发布配置构建
dotnet test Girvs.slnx                  # 运行全部 xUnit 测试
dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj
```

仅修改一个模块时，优先构建其 `.csproj`。修改 Aspire 编排或网关逻辑时，同时运行对应的 `Girvs.Aspire.*.Tests` 项目。

## 编码风格与命名

使用 4 空格缩进、文件作用域命名空间和 `ImplicitUsings`。类型、成员和枚举使用 `PascalCase`；接口以 `I` 开头；私有字段使用 `_camelCase`；参数与局部变量使用 `camelCase`。跨模块公共 using 放入 `GlobalUsings.cs`，文件专属 using 放在文件开头。异步 API 返回 `Task`/`Task<T>`，避免 `async void`。业务异常使用 `GirvsException`，日志使用 `ILogger<T>` 的结构化模板，例如 `"服务 {ServiceName} 已发现"`。

## 测试指南

测试框架为 xUnit。测试类以 `Tests` 结尾，测试方法描述行为与预期，例如 `AddGirvsGateway_使用Consul源_注册服务发现提供程序`。新增功能或缺陷修复必须覆盖成功路径及关键边界条件；涉及资源编排、共享配置分发或服务发现时，补充相应 Aspire、Hosting 或 Gateway 测试。

## 提交与 Pull Request

提交历史采用 Conventional Commits 风格，常见前缀为 `feat:`、`fix:`、`test:`、`docs:`，摘要使用简洁中文并说明模块，例如 `fix: 网关发现源改用结构化日志`。PR 应说明修改范围、验证命令及结果，并关联 Issue；变更示例应用、网关路由或配置行为时，附上必要的日志、截图或复现步骤。避免在同一 PR 混入无关格式化或重构。

## 配置与兼容性

所有包版本由 `Directory.Build.props` 统一管理，修改依赖时必须确认三个目标框架的兼容性。不要提交密钥、连接串或环境专属配置；示例配置应使用占位值。Aspire 与 Consul 服务发现互斥，修改两者接入逻辑前应验证目标部署模式。
