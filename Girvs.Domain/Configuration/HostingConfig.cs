namespace Girvs.Domain.Configuration
{
    public partial class HostingConfig
    {
        public string ForwardedHttpHeader { get; set; }

        public bool UseHttpClusterHttps { get; set; }

        public bool UseHttpXForwardedProto { get; set; }
    }
}