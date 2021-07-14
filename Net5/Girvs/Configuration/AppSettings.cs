using System.Collections.Generic;

namespace Girvs.Configuration
{
    public class AppSettings
    {
        public CommonConfig CommonConfig { get; set; } = new CommonConfig();

        public HostingConfig HostingConfig { get; set; } = new HostingConfig();

        public dynamic this[string index]
        {
            get => ModuleConfigurations[index];
            set => ModuleConfigurations[index] = value;
        }
        public IDictionary<string, dynamic> ModuleConfigurations { get; private set; } = null;

        public void PreLoadModelConfig()
        {
            ModuleConfigurations = new Dictionary<string, dynamic>();
        }
    }
}