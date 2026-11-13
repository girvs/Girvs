using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Girvs;

/// <summary>
/// SM4 对称加密（ECB 模式 + PKCS#7 填充）。
/// 密钥固定为 16 字节；字符串明文与密文分别按 UTF-8 与 Base64 处理。
/// </summary>
public static class Sm4Helper
{
    private const int Sm4BlockSize = 16;

    /// <summary>
    /// 使用配置中的 <see cref="CommonConfig.Sm4SecretKey"/> 加密。
    /// </summary>
    public static string Sm4Encrypt_ECB(this string plainText)
    {
        return Sm4Encrypt_ECB(plainText, GetConfiguredSecretKey());
    }

    /// <summary>
    /// 使用 UTF-8 编码的密钥加密。密钥长度必须为 16 字节。
    /// </summary>
    public static string Sm4Encrypt_ECB(this string plainText, string secretKey)
    {
        ArgumentNullException.ThrowIfNull(secretKey);
        return Sm4Encrypt_ECB(plainText, Encoding.UTF8.GetBytes(secretKey));
    }

    /// <summary>
    /// 使用原始密钥字节加密。密钥长度必须为 16 字节。
    /// </summary>
    public static string Sm4Encrypt_ECB(this string plainText, ReadOnlySpan<byte> key)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        var keyBytes = ValidateKey(key);
        var input = Encoding.UTF8.GetBytes(plainText);
        var cipher = CreateCipher(forEncryption: true, keyBytes);
        var output = new byte[cipher.GetOutputSize(input.Length)];
        var length = cipher.ProcessBytes(input, 0, input.Length, output, 0);
        length += cipher.DoFinal(output, length);

        return Convert.ToBase64String(output, 0, length);
    }

    /// <summary>
    /// 使用配置中的 <see cref="CommonConfig.Sm4SecretKey"/> 解密。
    /// </summary>
    public static string Sm4Decrypt_ECB(this string cipherText)
    {
        return Sm4Decrypt_ECB(cipherText, GetConfiguredSecretKey());
    }

    /// <summary>
    /// 使用 UTF-8 编码的密钥解密。密钥长度必须为 16 字节。
    /// </summary>
    public static string Sm4Decrypt_ECB(this string cipherText, string secretKey)
    {
        ArgumentNullException.ThrowIfNull(secretKey);
        return Sm4Decrypt_ECB(cipherText, Encoding.UTF8.GetBytes(secretKey));
    }

    /// <summary>
    /// 使用原始密钥字节解密。密钥长度必须为 16 字节。
    /// </summary>
    public static string Sm4Decrypt_ECB(this string cipherText, ReadOnlySpan<byte> key)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        var keyBytes = ValidateKey(key);
        var input = Convert.FromBase64String(cipherText);
        var cipher = CreateCipher(forEncryption: false, keyBytes);
        var output = new byte[cipher.GetOutputSize(input.Length)];

        try
        {
            var length = cipher.ProcessBytes(input, 0, input.Length, output, 0);
            length += cipher.DoFinal(output, length);
            return Encoding.UTF8.GetString(output, 0, length);
        }
        catch (CryptoException exception)
        {
            throw new GirvsException("SM4 解密失败：密文或密钥不正确", exception);
        }
    }

    private static byte[] ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != Sm4BlockSize)
        {
            throw new GirvsException($"SM4 密钥长度必须为 {Sm4BlockSize} 字节，当前为 {key.Length} 字节");
        }

        return key.ToArray();
    }

    private static PaddedBufferedBlockCipher CreateCipher(bool forEncryption, byte[] key)
    {
        var cipher = new PaddedBufferedBlockCipher(new SM4Engine(), new Pkcs7Padding());
        cipher.Init(forEncryption, new KeyParameter(key));
        return cipher;
    }

    private static string GetConfiguredSecretKey()
    {
        var secretKey = Singleton<AppSettings>.Instance?.CommonConfig?.Sm4SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new GirvsException("未配置 SM4 密钥，请设置 CommonConfig:Sm4SecretKey");
        }

        return secretKey;
    }
}
