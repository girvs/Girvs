namespace Girvs.Quartz.Tests;

// 验证 AddQuartzHosted 对默认 JobFactory 的替换在 Quartz DI 集成下真实生效
public class QuartzExtensionsJobFactoryReplacementTests
{
    [Fact]
    public void AddQuartzHosted_将默认JobFactory替换为GirvsJobFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<RegisteredTestJob>();

        services.AddQuartzHosted(new QuartzConfiguration
        {
            Tasks =
            [
                new TaskConfig
                {
                    Name = "test-job",
                    Enabled = true,
                    Type = typeof(RegisteredTestJob).AssemblyQualifiedName,
                    CronExpression = "0 0 2 * * ?"
                }
            ]
        });

        // AddQuartz 注册的默认实现已被移除，仅剩 GirvsJobFactory 一个 IJobFactory 描述符
        var jobFactoryDescriptors = services
            .Where(d => d.ServiceType == typeof(IJobFactory))
            .ToList();
        Assert.Single(jobFactoryDescriptors);
        Assert.Equal(typeof(GirvsJobFactory), jobFactoryDescriptors[0].ImplementationType);

        // 从容器解析出的就是 GirvsJobFactory
        using var provider = services.BuildServiceProvider();
        Assert.IsType<GirvsJobFactory>(provider.GetRequiredService<IJobFactory>());
    }

    private sealed class RegisteredTestJob : GirvsJob
    {
        public override Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
