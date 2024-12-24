using System.Linq;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.CodeGenerator;

public class CodeGeneratorModule : IAppModuleStartup
{
    // private void RegisterSafeTypeWithAllProperties(Type type)
    // {
    //     Template.RegisterSafeType(type,
    //         type
    //             .GetProperties()
    //             .Select(p => p.Name)
    //             .ToArray(), o => o.ToString());
    // }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // RegisterSafeTypeWithAllProperties(typeof(TemplateParameter));
        // RegisterSafeTypeWithAllProperties(typeof(TemplateFieldParameter));
        // RegisterSafeTypeWithAllProperties(typeof(TemplateNamespaceParameter));
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = int.MaxValue;
}
