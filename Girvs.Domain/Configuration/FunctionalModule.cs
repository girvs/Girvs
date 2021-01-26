using System.Collections.Generic;

namespace Girvs.Domain.Configuration
{
    public class FunctionalModule
    {
        public FunctionalModule()
        {
            Configs = new List<FunctionalModuleConfig>();
        }
        /// <summary>
        /// 模块名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; }


        public List<FunctionalModuleConfig> Configs { get; set; }
    }


    public class FunctionalModuleConfig
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public double Timeout { get; set; }
    }
}