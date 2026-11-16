Status: needs-triage
Blocked by: 无

# P2：保留宿主内存缓存配置与注册

详见 [诊断报告](../report.md) 第 3 项。

验收：内存模式和同步内存模式遵守明确的缓存所有权约定；不隐式覆盖宿主 MemoryCacheOptions 或自定义 IMemoryCache。

## Comments

2026-11-16：宿主 SizeLimit 被忽略已复现，标准 AddMemoryCache 对照组符合配置。
