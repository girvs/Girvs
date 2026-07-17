using Girvs.Aspire.Hosting;
using Sample.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// 自定义资源类型扩展示例:注册 SQLite(无容器,见 SqliteResource.cs)
// 与 MongoDB(官方集成包容器,见 MongoSettingsProvider.cs)两种提供程序,
// 内置预设之外的资源类型都用这种方式接入
GirvsResourceSettingsProviders.Register(new SqliteSettingsProvider());
GirvsResourceSettingsProviders.Register(new MongoSettingsProvider());

// 显式编排基础资源并登记为 Girvs 资源:资源名即各服务 ConnectionRef 引用的键。
// 服务用不用、用哪个,由服务自己的 appsettings 决定,AppHost 不感知。
builder.AddRedis("sample-redis").AsGirvsResource(type: "redis-synchronized-memory");
builder.AddMySql("sample-mysql-server").AddDatabase("sample-mysql", databaseName: "sample").AsGirvsResource();
builder.AddRabbitMQ("sample-rabbitmq").AsGirvsResource();

// 本地文件型资源:无容器、无生命周期(IResourceWithoutLifetime,不会被 WaitFor)
builder.AddSqlite("sample-sqlite", Path.Combine(builder.AppHostDirectory, "obj", "sample.db"))
    .AsGirvsResource();

// 官方集成包容器资源:提取逻辑由上面注册的 MongoSettingsProvider 完成
builder.AddMongoDB("sample-mongo").AsGirvsResource();

var serviceB = builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b");

// service-a 引用 service-b:注入其发现地址,供 service-a 用 http://service-b 通过服务发现调用
builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a").WithReference(serviceB);

// 后台工作服务:AddGirvsProject 对含 BackgroundService 的服务同样适用
builder.AddGirvsProject<Projects.Sample_Worker>("worker");

builder.Build().Run();
