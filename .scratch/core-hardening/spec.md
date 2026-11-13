# 核心库上策加固

## 目标

修复 Girvs 核心库中影响正确性与安全性的高优先级问题，并在 `net8.0;net9.0;net10.0;net11.0` 四个目标下保持可构建、可测试。

## 范围决策

- SM4：直接替换旧实现，不保留双轨；密文格式变为标准 Base64。
- `GirvsException`：直接删除 `[Serializable]` 与 `SerializationInfo` 构造。
- 目标框架：四目标全部保留，不移除 net8/net9。
- 测试：`Girvs.Core.Tests` 扩展为四目标。

## 阶段

1. 基线、测试护栏与四目标测试扩展。
2. 低风险独立修复：`ServicePointManager`、`KnownIPNetworks`、异常序列化构造、请求扩展、配置入口、`HashHelper`。
3. 文件提供程序。
4. 密码学：RSA 资源释放、SM4 直接替换、默认密钥。
5. DI 与静态状态。
6. 实体构造副作用。
7. 四目标回归与文档。

## 约束

- 每阶段独立提交、独立验证。
- 外部数据兼容性变更写入 `框架升级说明.md`。
- 不发布 NuGet 包。
