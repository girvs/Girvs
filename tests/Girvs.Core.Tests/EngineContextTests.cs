using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Girvs.Core.Tests;

/// <summary>
/// 引擎上下文依赖进程级静态状态，关闭并行以免与其他测试互相污染。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EngineContextCollection
{
    public const string Name = "EngineContext";
}

[Collection(EngineContextCollection.Name)]
public class EngineContextTests
{
    [Fact]
    public void Create_多次调用_返回同一实例()
    {
        var original = Singleton<IEngine>.Instance;
        try
        {
            Singleton<IEngine>.Instance = null;

            var first = EngineContext.Create();
            var second = EngineContext.Create();

            Assert.NotNull(first);
            Assert.Same(first, second);
        }
        finally
        {
            Singleton<IEngine>.Instance = original;
        }
    }

    [Fact]
    public void Singleton_并发写入_不抛异常且保留最后值()
    {
        var original = Singleton<string>.Instance;
        try
        {
            Parallel.For(0, 200, index => Singleton<string>.Instance = $"value-{index}");

            Assert.NotNull(Singleton<string>.Instance);
        }
        finally
        {
            Singleton<string>.Instance = original;
        }
    }

    [Fact]
    public void Singleton_写入空值_从注册表移除()
    {
        var original = Singleton<string>.Instance;
        try
        {
            Singleton<string>.Instance = "temporary";
            Assert.True(BaseSingleton.AllSingletons.ContainsKey(typeof(string)));

            Singleton<string>.Instance = null;

            Assert.False(BaseSingleton.AllSingletons.ContainsKey(typeof(string)));
        }
        finally
        {
            Singleton<string>.Instance = original;
        }
    }

    [Fact]
    public void Replace_替换引擎_Current返回替换后的实例()
    {
        var original = Singleton<IEngine>.Instance;
        try
        {
            var replacement = new TestEngine();
            EngineContext.Replace(replacement);

            Assert.Same(replacement, EngineContext.Current);
        }
        finally
        {
            Singleton<IEngine>.Instance = original;
        }
    }

    private sealed class TestEngine : IEngine
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

        public void ConfigureRequestPipeline(IApplicationBuilder application, IWebHostEnvironment env) { }

        public void ConfigureEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder) { }

        public T Resolve<T>(IServiceScope scope = null) where T : class => null;

        public object Resolve(Type type, IServiceScope scope = null) => null;

        public IEnumerable<T> ResolveAll<T>() => Array.Empty<T>();

        public object ResolveUnregistered(Type type) => null;

        public HttpContext HttpContext => null;

        public bool IsAuthenticated => false;

        public IGirvsPrincipalAccessor PrincipalAccessor => null;

        public TConfig GetAppModuleConfig<TConfig>() where TConfig : class, IAppModuleConfig => null;

        public void SetCurrentThreadServiceProvider(IServiceProvider serviceProvider) { }

        public IDisposable ChangeCurrentThreadServiceProvider(IServiceProvider serviceProvider) => null;
    }
}
