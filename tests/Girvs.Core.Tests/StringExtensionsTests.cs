using Girvs.Extensions;
using Xunit;

namespace Girvs.Core.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("abc", "900150983CD24FB0D6963F7D28E17F72", "3CD24FB0D6963F7D")]
    [InlineData("中文", "A7BAC2239FCDCB3A067903D8077C4A07", "9FCDCB3A067903D8")]
    public void Md5扩展_返回大写十六进制摘要(string input, string md5, string shortMd5)
    {
        Assert.Equal(md5, input.ToMd5());
        Assert.Equal(shortMd5, input.ToShortMd5());
    }

    [Fact]
    public void Md5扩展_空字符串和Null_保留各自行为()
    {
        string? missing = null;

        Assert.Equal(string.Empty, missing.ToMd5());
        Assert.Equal(string.Empty, string.Empty.ToMd5());
        Assert.Equal("8F00B204E9800998", string.Empty.ToShortMd5());
        Assert.Throws<ArgumentNullException>(() => missing.ToShortMd5());
    }
}
