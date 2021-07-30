namespace Girvs.Configuration
{
    public class HostingConfig : IConfig
    {
        /// <summary>
        /// 获取或设置一个值，该值指示是否使用HTTP_CLUSTER_HTTPS
        /// </summary>
        public bool UseHttpClusterHttps { get; set; } = false;

        /// <summary>
        /// 获取或设置一个值，该值指示是否使用HTTP_X_FORWARDED_PROTO
        /// </summary>
        public bool UseHttpXForwardedProto { get; set; } = false;

        /// <summary>
        /// 获取或设置自定义转发的HTTP标头（例如CF-Connecting-IP，X-FORWARDED-PROTO等）
        /// </summary>
        public string ForwardedHttpHeader { get; set; } = string.Empty;
    }
}