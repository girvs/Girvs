using System.IO;
using Girvs;
using Girvs.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Core;
using Serilog.Sinks.TestCorrelator;
using Xunit;

namespace Girvs.Core.Tests;

/// <summary>
/// Console sink 通过 Console.SetOut 捕获全局标准输出，禁止并行以防相互污染。
/// </summary>
[Collection("ConsoleSink")]
public class GirvsHostBuilderManagerTests
{
    [Fact]
    public void HostUseSerilog_无业务Serilog配置_Information日志写入Console且格式包含INF与消息()
    {
        var originalOut = Console.Out;
        var consoleCapture = new StringWriter();
        Console.SetOut(consoleCapture);

        try
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(
                    (_, config) =>
                    {
                        config.Sources.Clear();
                        config.AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["Environment"] = "Test",
                            }
                        );
                    }
                );

            hostBuilder.HostUseSerilog();
            var host = hostBuilder.Build();

            using (host)
            {
                var logger = host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>();

                // 默认最低级别为 Information，Debug 日志不应输出到 Console
                logger.LogDebug("默认 Debug 不应输出");

                // Microsoft/System 命名空间被覆盖为 Warning，其 Information 日志不应输出
                var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                loggerFactory.CreateLogger("Microsoft.Test").LogInformation("Microsoft Information 不应输出");
                loggerFactory.CreateLogger("System.Test").LogInformation("System Information 不应输出");

                // 兜底 Information 日志应输出
                logger.LogInformation("人眼可读兜底消息");
            }

            // Serilog Console sink 同步写入，强制刷新捕获缓冲
            var output = consoleCapture.ToString();

            // 默认级别与命名空间覆盖断言：防止级别或 Override 回归
            Assert.DoesNotContain("默认 Debug 不应输出", output);
            Assert.DoesNotContain("Microsoft Information 不应输出", output);
            Assert.DoesNotContain("System Information 不应输出", output);

            // 默认模板与级别断言：防止模板、级别缩写或 ||end 回归
            Assert.Contains("INF", output);
            Assert.Contains("人眼可读兜底消息", output);
            Assert.DoesNotContain("||end", output);

            // 时间格式正则断言（不断言具体时间值）
            Assert.Matches(
                @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \|\| \[INF\] \|\| ",
                output
            );
            Assert.Contains(" || 人眼可读兜底消息 || ", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void HostUseSerilog_业务配置追加TestCorrelator并下调Debug级别_Debug日志被TestCorrelator接收()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration(
                (_, config) =>
                {
                    config.Sources.Clear();
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Serilog:MinimumLevel:Default"] = "Debug",
                            ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                        }
                    );
                }
            );

        hostBuilder.HostUseSerilog();
        var host = hostBuilder.Build();

        using (host)
        using (TestCorrelator.CreateContext())
        {
            var logger = host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>();
            logger.LogDebug("业务调试消息");

            Assert.Contains(
                TestCorrelator.GetLogEventsFromCurrentContext(),
                le => le.Level == LogEventLevel.Debug && le.MessageTemplate.Text == "业务调试消息"
            );
        }
    }

    [Fact]
    public void HostUseSerilog_代码注册TestCorrelatorSink_Information日志被接收()
    {
        var sink = new CollectingSink();
        var services = new ServiceCollection();
        services.AddSerilogSink(
            configuration => configuration.WriteTo.Sink(sink),
            "CollectingSink"
        );

        var hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection([]);
            });
        hostBuilder.HostUseSerilog();

        using var host = hostBuilder.Build();
        host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
            .LogInformation("代码注册 Sink 日志");

        Assert.Contains(sink.Events, logEvent => logEvent.MessageTemplate.Text == "代码注册 Sink 日志");
    }

    [Fact]
    public void HostUseSerilog_代码声明覆盖TestCorrelator_配置Sink不接收日志()
    {
        var services = new ServiceCollection();
        services.AddSerilogSink(_ => { }, "TestCorrelator");

        var hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                    }
                );
            });
        hostBuilder.HostUseSerilog();

        using var host = hostBuilder.Build();
        using var context = TestCorrelator.CreateContext();
        host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
            .LogInformation("被覆盖的配置 Sink 不应接收");

        Assert.DoesNotContain(
            TestCorrelator.GetLogEventsFromCurrentContext(),
            logEvent => logEvent.MessageTemplate.Text == "被覆盖的配置 Sink 不应接收"
        );
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
