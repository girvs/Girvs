using Girvs.Aspire.Hosting;
using Sample.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// 自定义资源类型扩展:SQLite(无容器,见 SqliteResource.cs)与
// MongoDB(官方集成包容器,见 MongoSettingsProvider.cs)两个提供程序
// 实现 IGirvsResourceSettingsProvider 即被自动发现,无需注册。

// 显式编排基础资源并登记为 Girvs 资源:资源名即各服务 ConnectionRef 引用的键。
// 服务用不用、用哪个,由服务自己的 appsettings 决定,AppHost 不感知。
builder.AddRedis("sample-redis").AsGirvsResource(type: "redis-synchronized-memory");
builder.AddMySql("sample-mysql-server").AddDatabase("sample-mysql", databaseName: "sample").AsGirvsResource();
builder.AddRabbitMQ("sample-rabbitmq").AsGirvsResource();

var consul = builder
    .AddContainer("consul", "hashicorp/consul", "1.20")
    .WithArgs("agent", "-dev", "-client=0.0.0.0")
    .WithHttpEndpoint(targetPort: 8500, name: "http");

// 本地文件型资源:无容器、无生命周期(IResourceWithoutLifetime,不会被 WaitFor)
builder.AddSqlite("sample-sqlite", Path.Combine(builder.AppHostDirectory, "obj", "sample.db"))
    .AsGirvsResource();

// 官方集成包容器资源:提取逻辑由上面注册的 MongoSettingsProvider 完成
builder.AddMongoDB("sample-mongo").AsGirvsResource();

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b")
    .WaitFor(consul)
    .WithEnvironment("ModuleConfigurations__ConsulConfig__ServerName", "service-b")
    .WithEnvironment("ModuleConfigurations__ConsulConfig__ConsulAddress", consul.GetEndpoint("http"))
    .WithEnvironment("ModuleConfigurations__ConsulConfig__HealthAddress", "http://host.docker.internal:5102/Health");

// service-a 引用 service-b:注入其发现地址,供 service-a 用 http://service-b 通过服务发现调用
var serviceA = builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a")
    .WaitFor(consul)
    .WithReference(serviceB)
    .WithEnvironment("ModuleConfigurations__ConsulConfig__ServerName", "service-a")
    .WithEnvironment("ModuleConfigurations__ConsulConfig__ConsulAddress", consul.GetEndpoint("http"))
    .WithEnvironment("ModuleConfigurations__ConsulConfig__HealthAddress", "http://host.docker.internal:5101/Health");

builder.AddProject<Projects.Sample_Gateway>("gateway")
    .WaitFor(consul)
    .WithReference(serviceA)
    .WithReference(serviceB)
    .WithEnvironment("ConsulAddress", consul.GetEndpoint("http"));

// 后台工作服务:AddGirvsProject 对含 BackgroundService 的服务同样适用
builder.AddGirvsProject<Projects.Sample_Worker>("worker");

builder.Build().Run();
