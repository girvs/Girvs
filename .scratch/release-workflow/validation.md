# 标签发布工作流验证

日期：2026-11-16。

## 实现

新增 `.github/workflows/publish-nuget.yml`，延续 master 的两类标签规则：`v*` 与 `[0-9]*`。

- `actions/setup-dotnet@v4` 从 global.json 读取 11.0.100，同时安装 8.0.x、9.0.x、10.0.x，满足当前测试项目的运行时需求。[setup-dotnet 官方说明](https://github.com/actions/setup-dotnet#using-the-global-json-file-input) 明确两个输入可同时使用。
- Docker 预检；保留完整解决方案的还原、构建、测试和打包，不排除 Redis 集成测试。
- 标签版本经环境变量传入，去除可选 v 前缀；构建与打包均设置 Version、PackageVersion，关闭构建时自动打包。
- 包先上传 Actions artifact，再通过仓库 Secret NUGET_API_KEY 推送 NuGet；密钥仅在推送步骤注入，缺失时明确失败。

## 本地检查

1. 从 rhysd/actionlint 官方 v1.7.12 release 下载 Windows 版本并用官方 checksums 核验 SHA256；actionlint 检查工作流退出码为 0。
2. `dotnet build Girvs.slnx -c Release --no-restore -p:Version=11.0.0-preview1.0 -p:PackageVersion=11.0.0-preview1.0 -p:GeneratePackageOnBuild=false --verbosity quiet` 成功，216 个既有警告、0 个错误。
3. `dotnet pack Girvs.slnx -c Release --no-build --no-restore -p:Version=11.0.0-preview1.0 -p:PackageVersion=11.0.0-preview1.0 -p:GeneratePackageOnBuild=false -o .scratch/release-workflow/artifacts/packages --verbosity quiet` 成功。
4. 检查 17 个 nupkg：nuspec 版本全部为 11.0.0-preview1.0，全部含 lib/net11.0 程序集。包保留在被忽略的 artifacts 目录，不提交。
5. Release 配置的 Girvs.Aspire.Hosting.Tests 在 net10.0、net11.0 各 58/58 通过。

本机没有 .NET 8、9 运行时与 Docker，未宣称全套测试通过；工作流会在补齐环境的 GitHub Ubuntu runner 上执行完整测试。

## Standards

规范违规 0 项，需处理的代码异味 0 项。标签和密钥通过 env 传递，Shell 引用完整；SDK 与全套测试所需环境完整；权限只读，密钥未入库，产物缺失会失败。

## Spec

发现 0 项。标签触发、版本传递、SDK 安装与整仓还原、构建、测试、打包、推送均符合规格，没有过滤或跳过测试。

## 远端执行

工作流随 b7d3d6d 推送至 dev，标签从 b289929 安全更新至该提交，确认 [发布运行 #3](https://github.com/girvs/Girvs/actions/runs/43822530526) 成功触发。

SDK、Docker、还原与构建均成功，完整测试发现 FileProviderTests.Combine_多段路径_合并为系统路径 在四个框架下失败：预期 a/b/c.json，实际 /a/b/c.json。其他测试，包括两个框架的 Redis 集成测试，均通过；发布阶段尚未执行。

Git blame 确认 Unix 补根路径逻辑自 2022 年即存在，本次只修正跨平台测试预期，不修改库行为。修正后重新验证并更新标签触发完整发布。

修正随提交 `4455366` 推送，并将标签安全更新至该提交。本机 Release Core 测试在 net10.0、net11.0 各 86/86 通过，增量规范与需求审查均无发现。

[发布运行 #4](https://github.com/girvs/Girvs/actions/runs/43824307093) 实际完成环境检查、依赖还原、构建、完整测试、打包、Actions 产物上传；所有这些步骤结论均为 success。NuGet 推送步骤返回 HTTP 403，明确错误为：

```text
The specified API key has expired. Please create a new API key at https://www.nuget.org/account/apikeys.
```

当前剩余阻碍为过期的仓库 Secret NUGET_API_KEY，未宣称 NuGet 发布成功。维护者需要在 NuGet 生成具有所需包推送权限的新 API key，并在 GitHub 仓库 Settings → Secrets and variables → Actions 更新同名 Secret。随后可重跑运行 #4，无需再次改代码或移动标签。17 个包已通过 Actions artifact 步骤保存。

### 后续凭据核验

主公更新 Secret 后分别重跑 attempt 2、3，NuGet 仍明确返回过期；仓库 Secret 更新时间均早于对应运行，位置正确。主公再次更新后重跑 [attempt 4](https://github.com/girvs/Girvs/actions/runs/43824307093/attempts/4)，所有发布前步骤仍成功，推送返回 HTTP 403，错误改为密钥无效、过期或无包访问权限。

此时从账户页核对的新密钥确实未过期：Package owner 为 Girvs，Glob pattern 为 Girvs*，具有推送新包和版本的权限。但 [Girvs.Aspire.Hosting](https://www.nuget.org/packages/Girvs.Aspire.Hosting) 的 Owners 仅为 kicck，故该密钥的所有者范围无法授权此包。最新阻碍已变为所有者范围不匹配，不再认定当前新密钥过期。

需要创建或调整为 Package owner=kicck、Glob pattern=Girvs*、允许推送新包及版本的有效密钥，并将完整值更新至仓库 NUGET_API_KEY 后重跑。亮未创建、修改或轮换任何密钥。
