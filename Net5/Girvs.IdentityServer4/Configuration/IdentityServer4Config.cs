using Girvs.Configuration;


namespace Girvs.IdentityServer4.Configuration
{
    public class IdentityServer4Config : IAppModuleConfig
    {
        public string ServerHost { get; set; } = "http://localhost:5001";
        public string ClientName { get; set; } = "ApiName1";
        public bool UseHttps { get; set; } = false;
        public string ApiSecret { get; set; } = "ApiSecret";
    }
}