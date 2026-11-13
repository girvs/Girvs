using System.Text;
using Girvs;
using Xunit;

namespace Girvs.Core.Tests;

public class HashHelperTests
{
    [Fact]
    public void CreateHash_默认算法_与SHA1已知值一致()
    {
        var hash = HashHelper.CreateHash(Encoding.UTF8.GetBytes("abc"));

        Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", hash);
    }

    [Fact]
    public void CreateHash_Sha256_与已知值一致()
    {
        var hash = HashHelper.CreateHash(Encoding.UTF8.GetBytes("abc"), "SHA256");

        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
            hash
        );
    }

    [Fact]
    public void CreateHash_TrimByteCount_只对前缀计算哈希()
    {
        var data = Encoding.UTF8.GetBytes("abcdef");
        var full = HashHelper.CreateHash(data);
        var trimmed = HashHelper.CreateHash(data, "SHA1", 3);

        Assert.NotEqual(full, trimmed);
        Assert.Equal(HashHelper.CreateHash(Encoding.UTF8.GetBytes("abc")), trimmed);
    }

    [Fact]
    public void CreateHash_空算法名_抛出参数异常()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HashHelper.CreateHash(Encoding.UTF8.GetBytes("abc"), string.Empty)
        );
    }

    [Fact]
    public void CreateHash_未知算法_抛出参数异常()
    {
        Assert.Throws<ArgumentException>(() =>
            HashHelper.CreateHash(Encoding.UTF8.GetBytes("abc"), "NOT-A-HASH")
        );
    }

    [Fact]
    public void CreateHash_数据为空_抛出参数异常()
    {
        Assert.Throws<ArgumentNullException>(() => HashHelper.CreateHash(null));
    }
}
