using System.Collections.Concurrent;

namespace Girvs;

/// <summary>
/// RSA加解密 使用OpenSSL的公钥加密/私钥解密
/// 公私钥请使用openssl生成  ssh-keygen -t rsa 命令生成的公钥私钥是不行的
/// </summary>
public static class GirvsRsaHelper
{
    // PEM 解析（去头尾、Base64 解码、ASN.1 解析）成本较高，密钥字符串稳定时可复用解析结果。
    // 每次操作仍新建 RSA 实例，保证并发安全。
    private static readonly ConcurrentDictionary<string, RSAParameters> PublicKeyCache = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, RSAParameters> PrivateKeyCache = new(StringComparer.Ordinal);

    /// <summary>
    /// 解析X.509公钥（PEM格式）
    /// </summary>
    private static RSAParameters ParsePublicKey(string key)
    {
        var keyBytes = Convert.FromBase64String(
            NormalizeKey(key, "-----BEGIN PUBLIC KEY-----", "-----END PUBLIC KEY-----")
        );

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        return rsa.ExportParameters(includePrivateParameters: false);
    }

    /// <summary>
    /// 解析PKCS#1或PKCS#8私钥（PEM格式）
    /// </summary>
    private static RSAParameters ParsePrivateKey(string key)
    {
        var keyBytes = Convert.FromBase64String(
            NormalizeKey(
                key,
                "-----BEGIN PRIVATE KEY-----",
                "-----END PRIVATE KEY-----",
                "-----BEGIN RSA PRIVATE KEY-----",
                "-----END RSA PRIVATE KEY-----"
            )
        );

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
        }
        catch
        {
            rsa.ImportRSAPrivateKey(keyBytes, out _);
        }

        return rsa.ExportParameters(includePrivateParameters: true);
    }

    private static string NormalizeKey(string key, params string[] markers)
    {
        foreach (var marker in markers)
            key = key.Replace(marker, string.Empty);

        return key.Replace("\n", string.Empty).Replace("\r", string.Empty);
    }

    private static RSA CreatePublicKey(string key)
    {
        return CreateRsa(PublicKeyCache.GetOrAdd(key, static k => ParsePublicKey(k)));
    }

    private static RSA CreatePrivateKey(string key)
    {
        return CreateRsa(PrivateKeyCache.GetOrAdd(key, static k => ParsePrivateKey(k)));
    }

    private static RSA CreateRsa(RSAParameters parameters)
    {
        var rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        return rsa;
    }

    /// <summary>
    /// 使用RSA公钥加密
    /// </summary>
    public static string Encrypt(
        this string plainText,
        string publicKey,
        RSAEncryptionPadding padding
    )
    {
        using var rsa = CreatePublicKey(publicKey);
        var data = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(rsa.Encrypt(data, padding));
    }

    /// <summary>
    /// 使用RSA私钥解密
    /// </summary>
    public static string Decrypt(
        this string cipherText,
        string privateKey,
        RSAEncryptionPadding padding
    )
    {
        using var rsa = CreatePrivateKey(privateKey);
        var data = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(rsa.Decrypt(data, padding));
    }

    /// <summary>
    /// 使用RSA私钥签名（支持SHA1或SHA256）
    /// </summary>
    public static string Sign(
        this string plainText,
        string privateKey,
        HashAlgorithmName hashAlgorithm
    )
    {
        using var rsa = CreatePrivateKey(privateKey);
        var data = Encoding.UTF8.GetBytes(plainText);
        var signature = rsa.SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// 使用RSA公钥验证签名（支持SHA1或SHA256）
    /// </summary>
    public static bool Verify(
        this string plainText,
        string signature,
        string publicKey,
        HashAlgorithmName hashAlgorithm
    )
    {
        using var rsa = CreatePublicKey(publicKey);
        var data = Encoding.UTF8.GetBytes(plainText);
        var signatureBytes = Convert.FromBase64String(signature);
        return rsa.VerifyData(data, signatureBytes, hashAlgorithm, RSASignaturePadding.Pkcs1);
    }
}
