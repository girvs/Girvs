using Girvs.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// 显式编排基础资源并登记为 Girvs 资源:资源名即各服务 ConnectionRef 引用的键。
// 服务用不用、用哪个,由服务自己的 appsettings 决定,AppHost 不感知。
builder.AddRedis("sample-redis").AsGirvsResource(type: "redis-synchronized-memory");
builder.AddMySql("sample-mysql-server").AddDatabase("sample-mysql", databaseName: "sample").AsGirvsResource();
builder.AddRabbitMQ("sample-rabbitmq").AsGirvsResource();

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b");

// service-a 引用 service-b:注入其发现地址,供 service-a 用 http://service-b 通过服务发现调用
builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a").WithReference(serviceB);

// 后台工作服务:AddGirvsProject 对含 BackgroundService 的服务同样适用
builder.AddGirvsProject<Projects.Sample_Worker>("worker");

builder.Build().Run();
