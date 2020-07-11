using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Girvs.Domain.Http.Extensions
{
    /// <summary>
    /// 代表ISession的扩展
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// 将值设置为会话
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="session">Session</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// 从会话中获取值
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="session">Session</param>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (value == null)
                return default(T);

            return JsonSerializer.Deserialize<T>(value);
        }
    }
}