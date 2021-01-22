using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Plugins.Grpc
{
    public class GrpcStartup : IPluginStartup
    {
        public string Name { get; }= "Grpc";
        public bool Enabled
        {
            get
            {
                var config = EngineContext.Current.Resolve<GirvsConfig>();
                var funcConfig = config.FunctionalModules.SingleOrDefault(x => x.Name == Name);
                return funcConfig != null && funcConfig.Enabled;
            }
        }
        
        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GirvsExceptionInterceptor>();
            });
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            application.UseGrpcWeb(new GrpcWebOptions() {DefaultEnabled = true});
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            builder.AddEndpointRouteBuilderGrpcServices();
        }

        public int Order { get; } = 208;
    }
}