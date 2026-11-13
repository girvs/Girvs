# 01 — 基线与四目标测试护栏

Status: completed
Blocked by: 无

## 完成内容

- `Girvs.Core.Tests` 目标框架由 `net10.0;net11.0` 扩展为 `net8.0;net9.0;net10.0;net11.0`。
- 统一 `Girvs.csproj` 中 net8/net9 的 Serilog 依赖到 net10/net11 同版本，消除 net9 开发版包与旧版配置绑定差异。
- 新增测试：`RsaHelperTests`、`Sm4HelperTests`、`HashHelperTests`、`GirvsConvertTests`、`ConfigurationExtensionsTests`、`FileProviderTests`。

## 验证结果

| 目标 | 结果 |
|---|---|
| net8.0 | 37 通过（本机缺 .NET 8 运行时，使用 `DOTNET_ROLL_FORWARD=Major` 在 .NET 10 运行时执行） |
| net9.0 | 37 通过（同上，RollForward） |
| net10.0 | 37 通过 |
| net11.0 | 37 通过 |

命令：

```bash
DOTNET_ROLL_FORWARD=Major dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj -f net8.0
DOTNET_ROLL_FORWARD=Major dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj -f net9.0
dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj -f net10.0
dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj -f net11.0
```

## 说明

- 本机仅安装 .NET 10/11 运行时；net8/net9 测试通过 RollForward 在 10 运行时执行，属编译与逻辑验证，不等同于在 8/9 运行时上的行为验证。
- 后续每阶段均需重新执行四目标测试。
