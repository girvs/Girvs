using System.Collections.Generic;
using Girvs.Configuration;

namespace Girvs.Refit.Configuration
{
    public class RefitConfig : IAppModuleConfig
    {
        public string ConsulServiceHost { get; set; } = "192.168.51.98:8500";

        public Dictionary<string, string> ServiceAddress { get; set; } = new Dictionary<string, string>();

        public string this[string index] => ServiceAddress[index];

        public void Init()
        {
        }
    }
}