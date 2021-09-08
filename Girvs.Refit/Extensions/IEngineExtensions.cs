using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Girvs.Infrastructure;
using Girvs.JsonConverters;
using Girvs.Refit.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace Girvs.Refit.Extensions
{
    public static class IEngineExtensions
    {
        public static T RestService<T>(this IEngine engine)
        {
            var attr = typeof(T).GetCustomAttribute(typeof(RefitServiceAttribute)) as RefitServiceAttribute;
            if (attr == null || string.IsNullOrEmpty(attr.ServiceName))
            {
                throw new GirvsException("请求配置错误");
            }

            var config = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
            if (!config.ServiceAddress.ContainsKey(attr.ServiceName))
            {
                throw new GirvsException($"未配置{attr.ServiceName}的请求地址");
            }

            return global::Refit.RestService.For<T>(config[attr.ServiceName],
                new RefitSettings
                {
                    ContentSerializer = new NewtonsoftJsonContentSerializer(
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )
                });
        }
    }
}