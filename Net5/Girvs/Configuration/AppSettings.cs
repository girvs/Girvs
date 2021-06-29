﻿using System.Collections.Generic;
using Girvs.Domain.Configuration;

namespace Girvs.Configuration
{
    public class AppSettings
    {
        public bool IsInit { get; set; } = false;
        
        public CommonConfig CommonConfig { get; set; } = new CommonConfig();

        public HostingConfig HostingConfig { get; set; } = new HostingConfig();

        public ClaimValueConfig ClaimValueConfig { get; protected set; } = new ClaimValueConfig();
        public IDictionary<string, dynamic> ModuleConfigurations { get; private set; } = null;

        public void PreLoadModelConfig()
        {
            ModuleConfigurations = new Dictionary<string, dynamic>();
        }
    }
}