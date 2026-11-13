namespace Girvs;

/// <summary>
/// RSA加解密 使用OpenSSL的公钥加密/私钥解密
/// 公私钥请使用openssl生成  ssh-keygen -t rsa 命令生成的公钥私钥是不行的
/// </summary>
public static class GirvsRsaHelper
{
    /// <summary>
    /// 加载X.509公钥（PEM格式）
    /// </summary>
    private static RSA LoadPublicKey(string key)
    {
        var keyBytes = Convert.FromBase64String(
            NormalizeKey(key, "-----BEGIN PUBLIC KEY-----", "-----END PUBLIC KEY-----")
        );

        var rsa = RSA.Create();
        try
        {
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 加载PKCS#1或PKCS#8私钥（PEM格式）
    /// </summary>
    private static RSA LoadPrivateKey(string key)
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

        var rsa = RSA.Create();
        try
        {
            try
            {
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            }
            catch
            {
                rsa.ImportRSAPrivateKey(keyBytes, out _);
            }

            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static string NormalizeKey(string key, params string[] markers)
    {
        foreach (var marker in markers)
            key = key.Replace(marker, string.Empty);

        return key.Replace("\n", string.Empty).Replace("\r", string.Empty);
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
        using var rsa = LoadPublicKey(publicKey);
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
        using var rsa = LoadPrivateKey(privateKey);
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
        using var rsa = LoadPrivateKey(privateKey);
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
        using var rsa = LoadPublicKey(publicKey);
        var data = Encoding.UTF8.GetBytes(plainText);
        var signatureBytes = Convert.FromBase64String(signature);
        return rsa.VerifyData(data, signatureBytes, hashAlgorithm, RSASignaturePadding.Pkcs1);
    }
}
