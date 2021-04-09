using Girvs.Configuration;

namespace Girvs.WebApplication.Configuration
{
    public class WebApplicationConfig : IAppModelConfig
    {
        public bool IsOpenWindow { get; set; } = false;
    }
}