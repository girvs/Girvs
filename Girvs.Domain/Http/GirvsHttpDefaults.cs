namespace Girvs.Domain.Http
{
    /// <summary>
    /// 代表与HTTP功能有关的默认值
    /// </summary>
    public static partial class GirvsHttpDefaults
    {
        /// <summary>
        /// 获取默认HTTP客户端的名称
        /// </summary>
        public static string DefaultHttpClient => "default";

        /// <summary>
        /// 获取请求项的名称，该请求项的名称存储指示使用POST是否将客户端重定向到新位置的值
        /// </summary>
        public static string IsPostBeingDoneRequestItem => "sp.IsPOSTBeingDone";

        /// <summary>
        /// 获取HTTP_CLUSTER_HTTPS标头的名称
        /// </summary>
        public static string HttpClusterHttpsHeader => "HTTP_CLUSTER_HTTPS";

        /// <summary>
        /// 获取HTTP_X_FORWARDED_PROTO标头的名称
        /// </summary>
        public static string HttpXForwardedProtoHeader => "X-Forwarded-Proto";

        /// <summary>
        /// 获取X-FORWARDED-FOR标头的名称
        /// </summary>
        public static string XForwardedForHeader => "X-FORWARDED-FOR";
    }
}