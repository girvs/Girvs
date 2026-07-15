using Girvs.Aspire.Hosting;
using Sample.Modules;

var builder = DistributedApplication.CreateBuilder(args);

// 共享配置：一处声明，所有 AddGirvsProject 的服务自动接收（非敏感→普通配置，敏感→K8s Secret）
var jwt = builder.AddParameter("jwt-secret", "sample-dev-secret");
builder.AddGirvsSharedConfiguration(shared =>
{
    shared.AddSetting("Logging:LogLevel:Default", "Warning");
    shared.AddSecret("Jwt:Secret", jwt);
});

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b", typeof(ServiceBModule));

// service-a 引用 service-b：注入其发现地址，供 service-a 用 http://service-b 通过服务发现调用
builder
    .AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule))
    .WithReference(serviceB);

builder.Build().Run();
