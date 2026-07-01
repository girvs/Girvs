using FluentValidation;
using Girvs;
using Girvs.Driven.Commands;
using Girvs.Driven.Extensions;
using Girvs.Driven.Validations;
using Girvs.FileProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

CommonHelper.DefaultFileProvider = new GirvsFileProvider(new TestWebHostEnvironment());

var services = new ServiceCollection();
services.RegisterIValidatorType();

// 启动期校验会立即暴露错误的开放泛型注册。
using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});
using var scope = provider.CreateScope();
var validator = scope.ServiceProvider.GetRequiredService<IValidator<RegistrationTestCommand>>();

if (validator.GetType() != typeof(RegistrationTestCommandValidator))
{
    throw new InvalidOperationException($"业务验证器注册异常：{validator.GetType().FullName}");
}

var defaultValidator = scope.ServiceProvider.GetRequiredService<IValidator<DefaultRegistrationTestCommand>>();
if (defaultValidator.GetType().GetGenericTypeDefinition() != typeof(GirvsDefaultCommandValidator<>))
{
    throw new InvalidOperationException($"默认验证器注册异常：{defaultValidator.GetType().FullName}");
}

Console.WriteLine("验证器注册测试通过。");

/// <summary>
/// 用于验证默认校验器注册的测试命令。
/// </summary>
public sealed record RegistrationTestCommand() : Command("验证器注册测试");

/// <summary>
/// 用于验证默认校验器兜底注册的测试命令。
/// </summary>
public sealed record DefaultRegistrationTestCommand() : Command("默认验证器注册测试");

/// <summary>
/// 用于验证业务自定义校验器优先于默认校验器。
/// </summary>
public sealed class RegistrationTestCommandValidator : GirvsCommandValidator<RegistrationTestCommand>
{
}

/// <summary>
/// 为控制台测试提供最小 Web 宿主环境。
/// </summary>
internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Girvs.Driven.RegistrationTests";

    public IFileProvider ContentRootFileProvider { get; set; } =
        new PhysicalFileProvider(AppContext.BaseDirectory);

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public string EnvironmentName { get; set; } = Environments.Development;

    public string WebRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider WebRootFileProvider { get; set; } =
        new PhysicalFileProvider(AppContext.BaseDirectory);
}
