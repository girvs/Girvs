Status: needs-info
Blocked by: NuGet API key 已过期，需要仓库维护者更新 NUGET_API_KEY Secret 后重跑发布。

# 补齐工作流并触发标签发布

- [x] 确认缺失的工作流及现有 master 配置。
- [x] 补齐 global.json SDK、测试运行时与 Docker 检查。
- [x] 校验工作流语法、Release 构建及版本打包。
- [x] 提交推送并安全更新 v11.0.0-preview1.0。
- [x] 查看实际 GitHub Actions 结果，记录任何阻碍。
- [ ] 仓库维护者更新过期的 NUGET_API_KEY Secret 后，重跑流水线完成 NuGet 推送。

## Comments

2026-11-16：确认主公授权补充；保持所有现有测试执行。

2026-11-16：发布运行 #3 已触发，SDK、Docker、还原和构建均通过。完整测试暴露既有文件路径测试未考虑 Unix 行为，修正其预期后重新触发；不跳过测试，不修改库源码。

2026-11-16：4455366 修正测试预期；本机 Core 测试 net10.0、net11.0 各 86/86 通过。标签更新后运行 #4（43824307093）的完整测试、打包与产物上传均成功。NuGet 首次推送返回 HTTP 403：The specified API key has expired。代码和触发问题已解决；等待维护者更新 Secret，之后可重跑同一工作流，无需再次更新标签。
