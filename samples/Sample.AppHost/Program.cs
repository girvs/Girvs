using Girvs.Aspire.Hosting;
using Sample.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// 自定义资源类型扩展:SQLite(无容器,见 SqliteResource.cs)与
// MongoDB(官方集成包容器,见 MongoSettingsProvider.cs)两个提供程序
// 实现 IGirvsResourceSettingsProvider 即被自动发现,无需注册。

// 显式编排基础资源并登记为 Girvs 资源:资源名即各服务 ConnectionRef 引用的键。
// 服务用不用、用哪个,由服务自己的 appsettings 决定,AppHost 不感知。
// builder.AddRedis("sample-redis").AsGirvsResource(type: "redis-synchronized-memory");
// builder.AddMySql("sample-mysql-server").AddDatabase("sample-mysql", databaseName: "sample").AsGirvsResource();
// builder.AddRabbitMQ("sample-rabbitmq").AsGirvsResource();
//
// // 本地文件型资源:无容器、无生命周期(IResourceWithoutLifetime,不会被 WaitFor)
// builder.AddSqlite("sample-sqlite", Path.Combine(builder.AppHostDirectory, "obj", "sample.db"))
//     .AsGirvsResource();

// 官方集成包容器资源:提取逻辑由上面注册的 MongoSettingsProvider 完成
// builder.AddMongoDB("sample-mongo").AsGirvsResource();

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>();

// ServiceA 引用 ServiceB：注入其发现地址，供 ServiceA 用 http://sample-serviceb 通过服务发现调用。
var serviceA = builder.AddGirvsProject<Projects.Sample_ServiceA>()
    .WithReference(serviceB);

var gateway = builder.AddProject<Projects.Sample_Gateway>("gateway")
    .WithReference(serviceA)
    .WithReference(serviceB);

builder.Build().Run();
