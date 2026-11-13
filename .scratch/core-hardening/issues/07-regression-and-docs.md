# 07 — 四目标回归与文档

Status: completed
Blocked by: 06

## 回归结果

| 项目 | 目标框架 | 结果 |
|---|---|---|
| `Girvs.Core.Tests` | net8.0 / net9.0 / net10.0 / net11.0 | 各 68 项通过 |
| `Girvs.Claims.Tests` | net10.0 / net11.0 | 各 46 项通过 |
| `Girvs.Aspire.Hosting.Tests` | net10.0 / net11.0 | 各 54 项通过 |
| `Girvs.EntityFrameworkCore.Tests` | net10.0 / net11.0 | 4 / 6 项通过 |
| `Girvs.Gateway.Tests` | net10.0 / net11.0 | 36 项通过，8 项跳过（环境） |
| `Girvs.OpenApi.Tests` | net10.0 / net11.0 | 各 6 项通过 |
| `Girvs.Refit.Tests` | net10.0 / net11.0 | 各 9 项通过 |
| `Girvs.ServiceGovernance.Tests` | net10.0 / net11.0 | 各 42 项通过 |
| `Sample.Gateway.Tests` | net10.0 | 1 项通过 |

- `dotnet build Girvs.slnx -t:Rebuild`：0 错误，297 警告（既有 Nullable/打包类警告为主）。
- net8/net9 测试通过 `DOTNET_ROLL_FORWARD=Major` 在 .NET 10 运行时执行，本机未安装 .NET 8/9 运行时。

## 环境限制

`Girvs.Gateway.Tests.RedisFlowStateStoreTests` 的 8 项失败均因本机 Docker 未运行，`Testcontainers` 无法启动 Redis，与代码改动无关。具备 Docker 的环境应重跑该测试类。

## 文档

`框架升级说明.md` 新增第 14 节「核心库 .NET 11 加固」，覆盖：

- 依赖变化（BouncyCastle、Serilog、异常序列化）；
- SM4 破坏性变更与存量密文迁移要求；
- RSA 资源释放；
- 主机入口命名与 `[Obsolete]` 兼容；
- 容器回填与启动阶段解析限制；
- 文件提供程序、实体构造、HashHelper、请求扩展等行为修正；
- 升级检查命令。
