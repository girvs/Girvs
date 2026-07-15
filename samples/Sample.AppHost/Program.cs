using Girvs.Aspire.Hosting;
using Sample.Modules;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddGirvsProject<Projects.Sample_ServiceA>("service-a", typeof(ServiceAModule));
builder.AddGirvsProject<Projects.Sample_ServiceB>("service-b", typeof(ServiceBModule));

builder.Build().Run();
