using Girvs.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Girvs.Core.Tests;

public class RequestExtensionsTests
{
    [Fact]
    public void IsMobileBrowser_UserAgent为空_返回False且不抛异常()
    {
        var context = new DefaultHttpContext();

        Assert.False(context.Request.IsMobileBrowser());
    }

    [Fact]
    public void IsMobileBrowser_UserAgent短于四字符_返回False且不抛异常()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "abc";

        Assert.False(context.Request.IsMobileBrowser());
    }

    [Fact]
    public void IsMobileBrowser_移动端UserAgent_返回True()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) Mobile Safari";

        Assert.True(context.Request.IsMobileBrowser());
    }

    [Fact]
    public void UserAgent_无请求头_返回空字符串()
    {
        var context = new DefaultHttpContext();

        Assert.Equal(string.Empty, context.Request.UserAgent());
    }

    [Fact]
    public void GetUserRemoteIpAddress_无远端地址_返回空字符串()
    {
        var context = new DefaultHttpContext();

        Assert.Equal(string.Empty, context.Request.GetUserRemoteIpAddress());
    }

    [Fact]
    public void GetApiGateWayRemoteIpAddress_有ClientIP头_返回头值()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["ClientIP"] = "203.0.113.10";

        Assert.Equal("203.0.113.10", context.Request.GetApiGateWayRemoteIpAddress());
    }

    [Fact]
    public void GetRequestClientAddress_无ClientAddress头_回退远端地址()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.7");

        Assert.Equal("198.51.100.7", context.Request.GetRequestClientAddress());
    }

    [Fact]
    public void GetUserIpAddress_无任何来源_返回空字符串且不抛异常()
    {
        var context = new DefaultHttpContext();

        Assert.Equal(string.Empty, context.Request.GetUserIpAddress());
    }
}
