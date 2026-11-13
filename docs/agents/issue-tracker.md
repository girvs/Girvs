# 本地 Markdown 任务跟踪

- 每项工作使用 `.scratch/<feature>/` 独立目录。
- 规格保存在 `spec.md`。
- 每张任务单独保存为 `issues/<NN>-<slug>.md`，从 01 编号。
- 文件顶部使用 `Status:` 记录状态，使用 `Blocked by:` 记录依赖。
- 讨论追加到文件末尾的 `## Comments`。
- 技能要求发布或读取任务时，操作对应本地文件。
- 决策探索使用 `map.md`；子任务记录 Type、Status 和依赖，解决后回写决策。
