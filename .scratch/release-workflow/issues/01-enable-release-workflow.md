Status: in-progress
Blocked by: 无

# 补齐工作流并触发标签发布

- [x] 确认缺失的工作流及现有 master 配置。
- [x] 补齐 global.json SDK、测试运行时与 Docker 检查。
- [x] 校验工作流语法、Release 构建及版本打包。
- [x] 提交推送并安全更新 v11.0.0-preview1.0。
- [ ] 查看实际 GitHub Actions 结果，记录任何阻碍。

## Comments

2026-11-16：确认主公授权补充；保持所有现有测试执行。

2026-11-16：发布运行 #3 已触发，SDK、Docker、还原和构建均通过。完整测试暴露既有文件路径测试未考虑 Unix 行为，修正其预期后重新触发；不跳过测试，不修改库源码。
