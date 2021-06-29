using Girvs.Configuration;

namespace Girvs.WebApplication.Configuration
{
    public class WebApplicationConfig : IAppModuleConfig
    {
        public bool IsOpenWindow { get; set; } = false;
        public void Init()
        {
            
        }
    }
}