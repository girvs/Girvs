using System.Security.Cryptography;
using Girvs;
using Xunit;

namespace Girvs.Core.Tests;

public class RsaHelperTests
{
    [Fact]
    public void Encrypt与Decrypt_Pkcs1填充_可以还原明文()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();

        const string plainText = "Girvs RSA 往返测试";

        var cipherText = plainText.Encrypt(publicKey, RSAEncryptionPadding.Pkcs1);
        var decrypted = cipherText.Decrypt(privateKey, RSAEncryptionPadding.Pkcs1);

        Assert.NotEqual(plainText, cipherText);
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt与Decrypt_OaepSha256填充_可以还原明文()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();

        const string plainText = "OAEP SHA256 测试";

        var cipherText = plainText.Encrypt(publicKey, RSAEncryptionPadding.OaepSHA256);
        var decrypted = cipherText.Decrypt(privateKey, RSAEncryptionPadding.OaepSHA256);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Sign与Verify_Sha256签名_原文验签通过且篡改后失败()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();

        const string plainText = "待签名内容";

        var signature = plainText.Sign(privateKey, HashAlgorithmName.SHA256);

        Assert.True(plainText.Verify(signature, publicKey, HashAlgorithmName.SHA256));
        Assert.False("被篡改的内容".Verify(signature, publicKey, HashAlgorithmName.SHA256));
    }

    [Fact]
    public void Encrypt_非法公钥_抛出异常()
    {
        Assert.ThrowsAny<Exception>(() =>
            "内容".Encrypt("not-a-pem-key", RSAEncryptionPadding.Pkcs1)
        );
    }
}
