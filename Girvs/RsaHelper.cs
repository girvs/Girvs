namespace Girvs;

/// <summary>
/// RSA加解密 使用OpenSSL的公钥加密/私钥解密
/// 公私钥请使用openssl生成  ssh-keygen -t rsa 命令生成的公钥私钥是不行的
/// </summary>
public static class GirvsRsaHelper
{
    /// <summary>
    /// 加载X.509或PKCS#1公钥（PEM格式）
    /// </summary>
    private static RSA LoadPublicKey(this string key)
    {
        key = key.Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "");
        var keyBytes = Convert.FromBase64String(key);

        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _); // X.509 格式
        return rsa;
    }

    /// <summary>
    /// 加载PKCS#1或PKCS#8私钥（PEM格式）
    /// </summary>
    private static RSA LoadPrivateKey(this string key)
    {
        key = key.Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "");
        var keyBytes = Convert.FromBase64String(key);

        var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(keyBytes, out _); // PKCS#8 格式
        }
        catch
        {
            rsa.ImportRSAPrivateKey(keyBytes, out _); // PKCS#1 格式
        }

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
        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = publicKey.LoadPublicKey().Encrypt(data, padding);
        return Convert.ToBase64String(encrypted);
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
        var data = Convert.FromBase64String(cipherText);
        var decrypted = privateKey.LoadPrivateKey().Decrypt(data, padding);
        return Encoding.UTF8.GetString(decrypted);
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
        var data = Encoding.UTF8.GetBytes(plainText);
        var signature = privateKey
            .LoadPrivateKey()
            .SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
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
        var data = Encoding.UTF8.GetBytes(plainText);
        var signatureBytes = Convert.FromBase64String(signature);
        return publicKey
            .LoadPublicKey()
            .VerifyData(data, signatureBytes, hashAlgorithm, RSASignaturePadding.Pkcs1);
    }
}
