Status: completed
Blocked by: 无

# 补齐工作流并触发标签发布

- [x] 确认缺失的工作流及现有 master 配置。
- [x] 补齐 global.json SDK、测试运行时与 Docker 检查。
- [x] 校验工作流语法、Release 构建及版本打包。
- [x] 提交推送并安全更新 v11.0.0-preview1.0。
- [x] 查看实际 GitHub Actions 结果，记录任何阻碍。
- [x] 仓库维护者更新过期的 NUGET_API_KEY Secret 后，重跑流水线完成 NuGet 推送。

## Comments

2026-11-16：确认主公授权补充；保持所有现有测试执行。

2026-11-16：发布运行 #3 已触发，SDK、Docker、还原和构建均通过。完整测试暴露既有文件路径测试未考虑 Unix 行为，修正其预期后重新触发；不跳过测试，不修改库源码。

2026-11-16：4455366 修正测试预期；本机 Core 测试 net10.0、net11.0 各 86/86 通过。标签更新后运行 #4（43824307093）的完整测试、打包与产物上传均成功。NuGet 首次推送返回 HTTP 403：The specified API key has expired。代码和触发问题已解决；等待维护者更新 Secret，之后可重跑同一工作流，无需再次更新标签。

2026-11-16：主公确认 NUGET_API_KEY 已更新；通过 GitHub 的 Re-run failed jobs 启动运行 #4 的 attempt 2，API 确认 queued，代码与标签仍为 4455366。等待实际推送结果。

2026-11-16：attempt 2 的完整测试、打包及产物上传通过，但 NuGet 再次返回 API key 已过期。随后确认 GitHub 的 Secret 更新时间早于重跑，NuGet 账户中原发布 key 仍显示 Expired。主公再次更新后，启动 attempt 3；代码和标签未改。

2026-11-16：attempt 3 的完整测试、打包、产物上传仍通过，NuGet 推送仍为 HTTP 403（API key 已过期）。确认仓库 Secret 于本轮排队前更新，配置位置正确；NuGet 当前发布 key 仍显示 Expired。已请求主公确认是否由亮新建 30 天、Girvs* 范围的密钥并同步 Secret；确认前不更改凭据。

2026-11-16：主公再次确认已更新 API key，并要求重试；已启动 attempt 4，API 确认 in_progress，提交仍为 4455366。亮未创建或轮换任何密钥。

2026-11-16：attempt 4 的构建、完整测试、打包和产物上传均通过。NuGet 错误已从明确的过期变为无效、过期或无包访问权限。进一步核对：当前新密钥未过期，但 Package owner 为 Girvs，Glob 为 Girvs*；被拒绝的 Girvs.Aspire.Hosting 包仅归 kicck 所有，密钥所有者范围不匹配。需使用 kicck 所有者范围的有效发布密钥，更新 Secret 后重跑。

2026-11-16：主公按建议更新后再次要求重试。已核对新密钥未过期，Package owner=kicck、Glob pattern=Girvs*，允许推送新包及版本；启动 attempt 5，API 确认 in_progress，提交与标签仍为 4455366。

2026-11-16：attempt 5 完整成功，构建、完整测试、打包、产物上传与 NuGet 推送所有步骤均为 success。推送日志确认多个包获得 NuGet Created 响应与 Your package was pushed。v11.0.0-preview1.0 对应的 17 库发布流程已完成，无需再改代码或标签。此前的过期与所有者范围问题已解除。
