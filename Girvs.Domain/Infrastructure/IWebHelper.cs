using Microsoft.AspNetCore.Http;

namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 代表网络助手
    /// </summary>
    public partial interface IWebHelper
    {
        /// <summary>
        /// 获取URL引荐来源网址（如果存在）
        /// </summary>
        /// <returns>URL referrer</returns>
        string GetUrlReferrer();

        /// <summary>
        /// 从HTTP上下文获取IP地址
        /// </summary>
        /// <returns>String of IP address</returns>
        string GetCurrentIpAddress();

        /// <summary>
        /// 获取此页面的URL
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <param name="useSsl">Value indicating whether to get SSL secured page URL. Pass null to determine automatically</param>
        /// <param name="lowercaseUrl">Value indicating whether to lowercase URL</param>
        /// <returns>Page URL</returns>
        string GetThisPageUrl(bool includeQueryString, bool? useSsl = null, bool lowercaseUrl = false);

        /// <summary>
        /// 获取一个值，该值指示当前连接是否安全
        /// </summary>
        /// <returns>True if it's secured, otherwise false</returns>
        bool IsCurrentConnectionSecured();

        /// <summary>
        /// 获取商店主机位置
        /// </summary>
        /// <param name="useSsl">Whether to get SSL secured URL</param>
        /// <returns>Store host location</returns>
        string GetStoreHost(bool useSsl);

        /// <summary>
        /// 获取商店位置
        /// </summary>
        /// <param name="useSsl">Whether to get SSL secured URL; pass null to determine automatically</param>
        /// <returns>Store location</returns>
        string GetStoreLocation(bool? useSsl = null);

        /// <summary>
        /// 如果请求的资源是CMS引擎不需要处理的典型资源之一，则返回true。
        /// </summary>
        /// <returns>True if the request targets a static resource file.</returns>
        bool IsStaticResource();

        /// <summary>
        /// 修改URL的查询字符串
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="key">Query parameter key to add</param>
        /// <param name="values">Query parameter values to add</param>
        /// <returns>New URL with passed query parameter</returns>
        string ModifyQueryString(string url, string key, params string[] values);

        /// <summary>
        /// 从网址中删除查询参数
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="key">Query parameter key to remove</param>
        /// <param name="value">Query parameter value to remove; pass null to remove all query parameters with the specified key</param>
        /// <returns>New URL without passed query parameter</returns>
        string RemoveQueryString(string url, string key, string value = null);

        /// <summary>
        /// 按名称获取查询字符串值
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="name">Query parameter name</param>
        /// <returns>Query string value</returns>
        T QueryString<T>(string name);

        /// <summary>
        /// 重新启动应用程序域
        /// </summary>
        /// <param name="makeRedirect">A value indicating whether we should made redirection after restart</param>
        void RestartAppDomain(bool makeRedirect = false);

        /// <summary>
        /// 获取一个值，该值指示是否将客户端重定向到新位置
        /// </summary>
        bool IsRequestBeingRedirected { get; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否使用POST将客户端重定向到新位置
        /// </summary>
        bool IsPostBeingDone { get; set; }

        /// <summary>
        /// 获取当前的HTTP请求协议
        /// </summary>
        string CurrentRequestProtocol { get; }

        /// <summary>
        /// 获取指定的HTTP请求URI是否引用本地主机。
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <returns>True, if HTTP request URI references to the local host</returns>
        bool IsLocalRequest(HttpRequest req);

        /// <summary>
        /// 获取原始路径和请求的完整查询
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <returns>Raw URL</returns>
        string GetRawUrl(HttpRequest request);

        /// <summary>
        /// 获取是否使用AJAX发出请求
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <returns>Result</returns>
        bool IsAjaxRequest(HttpRequest request);
    }
}