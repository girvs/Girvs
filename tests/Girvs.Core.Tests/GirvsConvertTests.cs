using Girvs;
using Xunit;

namespace Girvs.Core.Tests;

public class GirvsConvertTests
{
    [Theory]
    [InlineData("System.Int32", "123", 123)]
    [InlineData("System.Int64", "456", 456L)]
    [InlineData("System.Boolean", "true", true)]
    [InlineData("System.Double", "1.5", 1.5d)]
    public void ToSpecifiedType_基础数值类型_转换成功(string typeName, string value, object expected)
    {
        var result = GirvsConvert.ToSpecifiedType(typeName, value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSpecifiedType_Guid_转换成功()
    {
        var guid = Guid.NewGuid();

        var result = GirvsConvert.ToSpecifiedType("System.Guid", guid.ToString());

        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToSpecifiedType_泛型入口_转换成功()
    {
        Assert.Equal(123, GirvsConvert.ToSpecifiedType<int>("123"));
    }

    [Fact]
    public void ToSpecifiedType_空值_抛出GirvsException()
    {
        Assert.Throws<GirvsException>(() => GirvsConvert.ToSpecifiedType("System.Int32", null));
    }

    [Fact]
    public void ToSpecifiedType_未支持类型_抛出GirvsException()
    {
        Assert.Throws<GirvsException>(() =>
            GirvsConvert.ToSpecifiedType("System.UInt64", "123")
        );
    }

    [Fact]
    public void ToSpecifiedType_按Type转换可空Guid_成功()
    {
        var guid = Guid.NewGuid();

        var result = GirvsConvert.ToSpecifiedType(typeof(Guid?), guid.ToString());

        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToSpecifiedType_按Type转换字符串_原样返回()
    {
        Assert.Equal("abc", GirvsConvert.ToSpecifiedType(typeof(string), "abc"));
    }

    [Fact]
    public void ToSpecifiedType_按Type转换枚举_忽略大小写()
    {
        var result = GirvsConvert.ToSpecifiedType(typeof(SampleLevel), "high");

        Assert.Equal(SampleLevel.High, result);
    }

    [Fact]
    public void ToSpecifiedType_按Type转换失败_抛出格式异常()
    {
        Assert.Throws<FormatException>(() =>
            GirvsConvert.ToSpecifiedType(typeof(int), "not-a-number")
        );
    }

    private enum SampleLevel
    {
        Low = 0,
        High = 1
    }
}
