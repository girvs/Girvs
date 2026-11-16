Status: needs-triage
Blocked by: 无

# P3：释放模块自行创建的内存缓存

详见 [诊断报告](../report.md) 第 4 项。

验收：模块自行创建的 MemoryCache 随容器释放；宿主传入的外部缓存继续遵守其原有所有权。

## Comments

2026-11-16：模块缓存未随根容器释放已复现；可与内存缓存注册方式一起修复。
