using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Girvs.Cache.Tests.Caching;

public class DistributedCacheManagerTests
{
    private readonly IDistributedCache _distributedCache = new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions())
    );

    [Fact]
    public async Task SetAsync_写入多对多循环引用对象_不抛异常()
    {
        using var manager = CreateManager(_distributedCache);
        var key = new CacheKey("groups", cacheTime: 1);

        await manager.SetAsync(key, CreateCyclicGroup());

        var cached = await manager.GetAsync<Group>(key);
        Assert.NotNull(cached);
        Assert.Same(cached, Assert.Single(Assert.Single(cached.Members).Groups));
    }

    [Fact]
    public async Task SetAsync_写入循环引用对象_底层缓存JSON包含引用元数据()
    {
        using var manager = CreateManager(_distributedCache);
        var key = new CacheKey("groups", cacheTime: 1);

        await manager.SetAsync(key, CreateCyclicGroup());

        var json = await _distributedCache.GetStringAsync(key.Key);
        Assert.NotNull(json);
        Assert.Contains("\"$id\"", json);
        Assert.Contains("\"$ref\"", json);
    }

    [Fact]
    public async Task GetAsync_缓存未命中时获取循环引用对象_读回后引用还原为同一实例()
    {
        using var writer = CreateManager(_distributedCache);
        using var reader = CreateManager(_distributedCache);
        var key = new CacheKey("groups", cacheTime: 1);
        var group = CreateCyclicGroup();

        var acquired = await writer.GetAsync(key, () => group);
        var cached = await reader.GetAsync<Group>(
            key,
            () => Task.FromException<Group>(new InvalidOperationException("此时应命中分布式缓存。"))
        );

        Assert.Same(group, acquired);
        Assert.NotNull(cached);
        Assert.NotSame(group, cached);
        Assert.Same(cached, Assert.Single(Assert.Single(cached.Members).Groups));
    }

    [Fact]
    public async Task GetAsync_读取旧格式无元数据JSON_兼容成功()
    {
        using var manager = CreateManager(_distributedCache);
        var key = new CacheKey("groups", cacheTime: 1);
        await _distributedCache.SetStringAsync(
            key.Key,
            """{"Members":[{"Name":"旧成员","Groups":[]}]}"""
        );

        var cached = await manager.GetAsync<Group>(key);

        Assert.NotNull(cached);
        var member = Assert.Single(cached.Members);
        Assert.Equal("旧成员", member.Name);
        Assert.Empty(member.Groups);
    }

    private static MemoryDistributedCacheManager CreateManager(IDistributedCache distributedCache)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        appSettings.ModuleConfigurations[nameof(CacheConfig)] = new CacheConfig();

        return new MemoryDistributedCacheManager(
            appSettings,
            distributedCache,
            new CacheKeyManager(new ConcurrentTrie<byte>()),
            new ConcurrentTrie<object>()
        );
    }

    private static Group CreateCyclicGroup()
    {
        var group = new Group();
        var member = new Member { Name = "成员" };
        group.Members.Add(member);
        member.Groups.Add(group);
        return group;
    }

    private sealed class Group
    {
        public List<Member> Members { get; set; } = new();
    }

    private sealed class Member
    {
        public string Name { get; set; } = string.Empty;
        public List<Group> Groups { get; set; } = new();
    }
}
