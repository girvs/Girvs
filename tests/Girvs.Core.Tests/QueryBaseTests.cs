using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Queries;
using Xunit;

namespace Girvs.Core.Tests;

public class QueryBaseTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    [InlineData("中文:=", "Name DESC", true)]
    public void GetCacheKey_包含继承属性与可空属性_与原算法一致(
        string? filter, string? orderBy, bool useQueryFields)
    {
        var query = new FilterQuery
        {
            Filter = filter,
            Number = useQueryFields ? 42 : null,
            PageIndex = 3,
            PageSize = 50,
            IncludeRecordCount = false,
            QueryFields = useQueryFields ? ["Name", "", null, "Id"] : [],
            OrderBy = orderBy,
            RecordCount = 123
        };

        Assert.Equal(GetLegacyCacheKey(query), query.GetCacheKey());

        query.PageIndex++;
        query.Filter = "新值";
        Assert.Equal(GetLegacyCacheKey(query), query.GetCacheKey());
    }

    [Fact]
    public void GetCacheKey_属性读取抛错_保留反射异常包装()
    {
        var error = Assert.Throws<TargetInvocationException>(() => new ThrowingQuery().GetCacheKey());

        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public void GetCacheKey_标记索引器_保留反射参数异常()
    {
        Assert.Throws<TargetParameterCountException>(() => new IndexerQuery().GetCacheKey());
    }

    private static string GetLegacyCacheKey(QueryBase<TestEntity> query)
    {
        var queryType = query.GetType();
        var key = new StringBuilder(queryType.FullName);
        foreach (var property in queryType.GetProperties())
        {
            if (property.IsDefined(typeof(QueryCacheKeyAttribute), true))
            {
                key.Append(':').Append(property.Name).Append('=').Append(property.GetValue(query));
            }
        }

        if (query.QueryFields is { Length: > 0 })
        {
            key.Append("QueryFields:").AppendJoin(',', query.QueryFields);
        }

        if (!string.IsNullOrEmpty(query.OrderBy))
        {
            key.Append("OrderBy:").Append(query.OrderBy);
        }

        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(key.ToString())));
    }

    private sealed class TestEntity : Entity;

    private class TestQuery : QueryBase<TestEntity>
    {
        [QueryCacheKey]
        public int? Number { get; set; }

        public override Expression<Func<TestEntity, bool>> GetQueryWhere() => entity => true;
    }

    private sealed class FilterQuery : TestQuery
    {
        [QueryCacheKey]
        public string? Filter { get; set; }

        [QueryCacheKey]
        public static string Scope => "global";
    }

    private sealed class ThrowingQuery : TestQuery
    {
        [QueryCacheKey]
        public string Filter => throw new InvalidOperationException();
    }

    private sealed class IndexerQuery : TestQuery
    {
        [QueryCacheKey]
        public string this[int index] => index.ToString();
    }
}
