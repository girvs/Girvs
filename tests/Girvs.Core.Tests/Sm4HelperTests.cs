using Girvs;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Xunit;

namespace Girvs.Core.Tests;

public class Sm4HelperTests
{
    [Theory]
    [InlineData("1234567890123456", "Girvs SM4 测试")]
    [InlineData("abcdefghijklmnop", "中文与符号 !@# 混排")]
    [InlineData("0123456789abcdef", "")]
    public void Sm4Encrypt与Sm4Decrypt_同一密钥_可以还原明文(string secretKey, string plainText)
    {
        var cipherText = plainText.Sm4Encrypt_ECB(secretKey);
        var decrypted = cipherText.Sm4Decrypt_ECB(secretKey);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Sm4Encrypt_不同密钥_密文不同()
    {
        const string plainText = "同一明文";

        var first = plainText.Sm4Encrypt_ECB("1234567890123456");
        var second = plainText.Sm4Encrypt_ECB("abcdefghijklmnop");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Sm4Encrypt_空明文_返回空字符串()
    {
        Assert.Equal(string.Empty, string.Empty.Sm4Encrypt_ECB("1234567890123456"));
    }

    [Fact]
    public void Sm4Decrypt_空密文_返回空字符串()
    {
        Assert.Equal(string.Empty, string.Empty.Sm4Decrypt_ECB("1234567890123456"));
    }

    [Fact]
    public void Sm4Engine_标准向量_与GB_T_32907一致()
    {
        var key = Convert.FromHexString("0123456789abcdeffedcba9876543210");
        var plain = Convert.FromHexString("0123456789abcdeffedcba9876543210");
        var output = new byte[plain.Length];
        var engine = new SM4Engine();
        engine.Init(true, new KeyParameter(key));

        engine.ProcessBlock(plain, 0, output, 0);

        Assert.Equal("681EDF34D206965E86B3E94F536E4246", Convert.ToHexString(output));
    }

    [Fact]
    public void Sm4Encrypt_密钥长度不是16字节_抛出GirvsException()
    {
        Assert.Throws<GirvsException>(() => "内容".Sm4Encrypt_ECB("short-key"));
    }

    [Fact]
    public void Sm4Decrypt_密文长度不是分组倍数_抛出GirvsException()
    {
        var cipherText = Convert.ToBase64String(new byte[] { 1, 2, 3 });

        Assert.Throws<GirvsException>(() =>
            cipherText.Sm4Decrypt_ECB("1234567890123456")
        );
    }
}
