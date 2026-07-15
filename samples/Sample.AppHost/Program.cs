using Girvs.Aspire.Hosting;
using Sample.Modules;

var builder = DistributedApplication.CreateBuilder(args);

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b", typeof(ServiceBModule));

// service-a 引用 service-b：注入其发现地址，供 service-a 用 http://service-b 通过服务发现调用
builder
    .AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule))
    .WithReference(serviceB);

builder.Build().Run();
