using Girvs.Configuration;

namespace Girvs.Swagger.Configuration;

public class SwaggerConfig : IAppModuleConfig
{
    public void Init() { }

    public bool EnableSwagger { get; set; } = false;
}
