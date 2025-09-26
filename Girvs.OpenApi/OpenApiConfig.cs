// using System.Text.Json.Serialization;
// using Girvs.Configuration;
//
// namespace Girvs.OpenApi;
//
// public class OpenApiConfig : IAppModuleConfig
// {
//     public void Init() { }
//
//     [JsonConverter(typeof(JsonStringEnumConverter))]
//     public ArchitectureType ArchitectureType { get; set; } = ArchitectureType.Microservices;
// }
//
// /// <summary>
// /// 系统运行架构类型
// /// </summary>
// public enum ArchitectureType
// {
//     /// <summary>
//     /// 单体架构
//     /// </summary>
//     SingleType,
//
//     /// <summary>
//     /// 微服务架构
//     /// </summary>
//     Microservices
// }
