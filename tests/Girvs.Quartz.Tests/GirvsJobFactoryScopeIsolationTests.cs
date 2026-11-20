using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz.Impl.Triggers;

namespace Girvs.Quartz.Tests;

// 验证 GirvsJobFactory 的作用域隔离与 EngineContext 桥接行为：
// 每次触发创建独立作用域、执行期内 EngineContext 指向该作用域、执行结束后还原、
// 作用域随 ReturnJob 释放、异常原样上抛、并发触发互不串扰。
public class GirvsJobFactoryScopeIsolationTests
{
    [Fact]
    public async Task Execute_同一次触发内_两次经EngineContext解析的Scoped服务为同一实例()
    {
        var (factory, recorder) = CreateEnvironment(typeof(ScopeObservationJob));
        var wrapper = factory.NewJob(CreateBundle(typeof(ScopeObservationJob)), null);

        await wrapper.Execute(null);

        // 同一次触发内两次经 EngineContext 解析命中同一 Scoped 实例
        var observations = recorder.Observations.ToList();
        Assert.Equal(2, observations.Count);
        Assert.True(observations[0].IsFirstResolve);
        Assert.False(observations[1].IsFirstResolve);
        Assert.Equal(observations[0].ProbeId, observations[1].ProbeId);

        // 执行结束但未归还 Job 前，作用域尚未释放
        Assert.Equal(0, recorder.DisposedCount);

        factory.ReturnJob(wrapper);
        // 归还 Job 时释放本次触发的作用域
        Assert.Equal(1, recorder.DisposedCount);
    }

    [Fact]
    public async Task Execute_未注册的Job_在桥接作用域内构造并执行且归还时释放作用域()
    {
        var (factory, recorder) = CreateEnvironment();
        var wrapper = factory.NewJob(CreateBundle(typeof(UnregisteredJob)), null);

        try
        {
            await wrapper.Execute(null);

            var (injectedProbe, constructionProbe, executionProbe) = recorder.ActivationProbes;
            Assert.NotNull(injectedProbe);
            Assert.Same(injectedProbe, constructionProbe);
            Assert.Same(injectedProbe, executionProbe);
            Assert.Equal(0, recorder.DisposedCount);
        }
        finally
        {
            factory.ReturnJob(wrapper);
        }

        Assert.Equal(1, recorder.DisposedCount);
    }

    [Fact]
    public async Task Execute_两次触发间_Scoped服务为不同实例且各自释放()
    {
        var (factory, recorder) = CreateEnvironment(typeof(ScopeObservationJob));

        var wrapper1 = factory.NewJob(CreateBundle(typeof(ScopeObservationJob)), null);
        await wrapper1.Execute(null);
        factory.ReturnJob(wrapper1);

        var wrapper2 = factory.NewJob(CreateBundle(typeof(ScopeObservationJob)), null);
        await wrapper2.Execute(null);
        factory.ReturnJob(wrapper2);

        // 两次触发共 4 次解析：各自触发内一致、触发间互不相同
        var observations = recorder.Observations.ToList();
        Assert.Equal(4, observations.Count);
        Assert.Equal(2, observations.Select(o => o.ProbeId).Distinct().Count());
        Assert.Equal(observations[0].ProbeId, observations[1].ProbeId);
        Assert.Equal(observations[2].ProbeId, observations[3].ProbeId);
        Assert.NotEqual(observations[0].ProbeId, observations[2].ProbeId);
        // 两次作用域均被释放
        Assert.Equal(2, recorder.DisposedCount);
    }

    [Fact]
    public async Task Execute_期间EngineContext指向独立作用域_执行结束后还原()
    {
        var (factory, recorder) = CreateEnvironment(typeof(BridgeProbeJob));

        // 进入前：EngineContext 指向引擎容器（可解析引擎标记，无法解析 Scoped 探针）
        var engineMarkerBefore = EngineContext.Current.Resolve<EngineMarker>();
        Assert.NotNull(engineMarkerBefore);
        Assert.Null(EngineContext.Current.Resolve<ScopedProbe>());

        var wrapper = factory.NewJob(CreateBundle(typeof(BridgeProbeJob)), null);
        await wrapper.Execute(null);

        // 执行期内：EngineContext 已桥接到本次触发的 scope provider
        Assert.True(recorder.InScopeProbeResolved);
        Assert.True(recorder.InScopeEngineMarkerNull);

        // 执行结束后：AsyncLocal 桥接随 using 还原为进入前的引擎容器
        Assert.Null(EngineContext.Current.Resolve<ScopedProbe>());
        Assert.Same(engineMarkerBefore, EngineContext.Current.Resolve<EngineMarker>());

        factory.ReturnJob(wrapper);
    }

    [Fact]
    public async Task Execute_Job抛异常_异常原样上抛且作用域仍被释放()
    {
        var (factory, recorder) = CreateEnvironment(typeof(ThrowingJob));
        var wrapper = factory.NewJob(CreateBundle(typeof(ThrowingJob)), null);

        // 异常不被吞掉、不被包装，原样上抛交给 Quartz
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => wrapper.Execute(null));
        Assert.Equal("job-boom", exception.Message);

        // 异常路径下 AsyncLocal 桥接同样还原
        Assert.Null(EngineContext.Current.Resolve<ScopedProbe>());

        // 归还 Job 时作用域仍被正常释放
        factory.ReturnJob(wrapper);
        Assert.Equal(1, recorder.DisposedCount);
    }

    [Fact]
    public async Task Execute_并发触发互不串扰_各自命中独立作用域()
    {
        const int triggerCount = 8;
        var (factory, recorder) = CreateEnvironment(typeof(ScopeObservationJob));

        // 模拟 Quartz 并发触发：多个包装 Job 同时执行，彼此作用域必须隔离
        var wrappers = Enumerable.Range(0, triggerCount)
            .Select(_ => factory.NewJob(CreateBundle(typeof(ScopeObservationJob)), null))
            .ToList();

        await Task.WhenAll(wrappers.Select(async wrapper =>
        {
            await Task.Yield(); // 让并发流真实交错
            await wrapper.Execute(null);
        }));

        foreach (var wrapper in wrappers)
            factory.ReturnJob(wrapper);

        // 每次触发两次解析、共 16 次观察；若某次触发内两次解析不一致，独立 Id 数将大于触发数
        var observations = recorder.Observations.ToList();
        Assert.Equal(triggerCount * 2, observations.Count);
        Assert.Equal(triggerCount, observations.Select(o => o.ProbeId).Distinct().Count());
        // 并发作用下每个作用域各释放一次，互不遗漏、互不重复
        Assert.Equal(triggerCount, recorder.DisposedCount);
    }

    private static TriggerFiredBundle CreateBundle(Type jobType)
    {
        var jobDetail = JobBuilder.Create(jobType).WithIdentity("test-job").Build();
        var trigger = new SimpleTriggerImpl("test-trigger");
        return new TriggerFiredBundle(
            jobDetail, trigger, null, false, DateTimeOffset.UtcNow, null, null, null);
    }

    private static (GirvsJobFactory Factory, TestRecorder Recorder) CreateEnvironment(
        params Type[] jobTypes)
    {
        // 「根容器」：模拟生产宿主，注册 Scoped 探针与被测 Job
        var rootServices = new ServiceCollection();
        rootServices.AddSingleton<TestRecorder>();
        rootServices.AddScoped<ScopedProbe>();
        foreach (var jobType in jobTypes)
            rootServices.AddScoped(jobType, jobType);
        var rootProvider = rootServices.BuildServiceProvider();

        // 「引擎容器」：进入触发前 EngineContext 的兜底状态（未注册 Scoped 探针）
        var engineServices = new ServiceCollection();
        engineServices.AddSingleton(new EngineMarker("engine"));
        var engine = new TestEngine();
        engine.UseProvider(engineServices.BuildServiceProvider());
        EngineContext.Replace(engine);

        var factory = new GirvsJobFactory(
            rootProvider, NullLogger<GirvsJobFactory>.Instance);
        return (factory, rootProvider.GetRequiredService<TestRecorder>());
    }

    // 引擎容器标记：证明桥接期间与还原之后 EngineContext 指向的 provider 变化
    private sealed record EngineMarker(string Name);

    // 带 Id 与释放标记的 Scoped 探针服务：按 Id 与释放计数验证隔离与释放
    private sealed class ScopedProbe(TestRecorder recorder) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose() => recorder.ProbeDisposed();
    }

    private sealed class TestRecorder
    {
        private int _disposedCount;

        public ConcurrentQueue<(Guid ProbeId, bool IsFirstResolve)> Observations { get; } = [];
        public (ScopedProbe Injected, ScopedProbe Construction, ScopedProbe Execution) ActivationProbes { get; set; }
        public int DisposedCount => _disposedCount;
        public volatile bool InScopeProbeResolved;
        public volatile bool InScopeEngineMarkerNull;

        public void Observe(Guid probeId, bool firstResolve) =>
            Observations.Enqueue((probeId, firstResolve));

        public void ProbeDisposed() => Interlocked.Increment(ref _disposedCount);
    }

    private sealed class UnregisteredJob(ScopedProbe probe, TestRecorder recorder) : GirvsJob
    {
        private readonly ScopedProbe _constructionProbe = EngineContext.Current.Resolve<ScopedProbe>();

        public override Task Execute(IJobExecutionContext context)
        {
            recorder.ActivationProbes = (probe, _constructionProbe, EngineContext.Current.Resolve<ScopedProbe>());
            return Task.CompletedTask;
        }
    }

    // 经 EngineContext 两次解析 Scoped 探针并记录结果
    private sealed class ScopeObservationJob(TestRecorder recorder) : GirvsJob
    {
        public override Task Execute(IJobExecutionContext context)
        {
            var first = EngineContext.Current.Resolve<ScopedProbe>();
            recorder.Observe(first.Id, true);
            var second = EngineContext.Current.Resolve<ScopedProbe>();
            recorder.Observe(second.Id, false);
            return Task.CompletedTask;
        }
    }

    // 观察执行期内 EngineContext 指向：scope 内有探针、无引擎容器标记
    private sealed class BridgeProbeJob(TestRecorder recorder) : GirvsJob
    {
        public override Task Execute(IJobExecutionContext context)
        {
            recorder.InScopeProbeResolved = EngineContext.Current.Resolve<ScopedProbe>() != null;
            recorder.InScopeEngineMarkerNull = EngineContext.Current.Resolve<EngineMarker>() == null;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingJob : GirvsJob
    {
        public override Task Execute(IJobExecutionContext context)
        {
            // 先经 EngineContext 解析 Scoped 探针（模拟构造链取得 DbContext），再抛出异常
            _ = EngineContext.Current.Resolve<ScopedProbe>();
            throw new InvalidOperationException("job-boom");
        }
    }

    private sealed class TestEngine : GirvsEngine
    {
        public void UseProvider(IServiceProvider provider) => ServiceProvider = provider;
    }
}
