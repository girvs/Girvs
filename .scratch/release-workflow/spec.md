# 补齐 .NET 11 标签发布流水线

日期：2026-11-16。

问题：v11.0.0-preview1.0 指向 dev 的 b289929，该提交没有 .github/workflows。发布流水线仅存在于 master，且仍只安装 .NET 10 SDK。

主公已同意补齐工作流并触发此前请求的标签发布。

范围：在 dev 增加发布工作流，读取 global.json 安装 .NET 11 SDK；同时安装测试所需 .NET 8、9、10 SDK/运行时，保持完整解决方案构建、测试、打包和 NuGet 发布。GitHub 托管 Ubuntu runner 需提供 Docker，供现有 Testcontainers.Redis 测试使用。

版本来自标签，兼容 v 前缀和数字开头标签。发布密钥继续使用仓库 Secret NUGET_API_KEY，不写入代码。验证通过后提交推送 dev，并安全重指原标签至包含工作流的提交，观察 GitHub Actions 运行。

不修改库源码、目标框架或依赖，不过滤或跳过测试。与 .NET 11 支持整体验收中的数据库和示例任务分别记录。
