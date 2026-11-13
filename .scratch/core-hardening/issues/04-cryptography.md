# 04 — 密码学加固

Status: completed
Blocked by: 03

## 完成内容

- RSA 帮助类改为在每次加解密/签名后释放 `RSA` 实例；密钥导入失败时也先释放再抛出。
- SM4 依赖由 `Portable.BouncyCastle 1.9.0` 替换为 `BouncyCastle.Cryptography 2.4.0`，四目标统一。
- 删除自实现 `Sm4Crypto` 与 `Sm4Context`，改用 `SM4Engine` + `PaddedBufferedBlockCipher` + `Pkcs7Padding`。
- SM4 公开 API 直接替换：移除 `secretKeyHexString` 参数；新增 `ReadOnlySpan<byte>` 密钥重载；密钥固定 16 字节；密文改为标准 Base64。
- 解密失败统一抛 `GirvsException`。
- `CommonConfig.Sm4SecretKey` 默认值改为空字符串；无参加解密在密钥缺失时抛 `GirvsException`。

## 破坏性变更

- 旧密文格式 `Base64(Hex(密文))` 不再可解；调用方需在升级前完成存量数据迁移。
- `Sm4Crypto`、`Sm4Context` 公共类型已移除。
- `Sm4Encrypt_ECB`/`Sm4Decrypt_ECB` 的 `secretKeyHexString` 参数已移除。

## 验证结果

- GB/T 32907-2016 标准向量通过。
- 密钥长度、分组长度异常路径测试通过。
- `Girvs.Core.Tests` 四目标各 54 项通过。
