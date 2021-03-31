using System.Collections.Generic;

namespace Girvs.Configuration
{
    public class AppSettings
    {
        public HostingConfig HostingConfig { get; set; } = new HostingConfig();
        
        public CacheConfig CacheConfig { get; set; } = new CacheConfig();

        public IDictionary<string, dynamic> AdditionalData { get; set; }
    }
}