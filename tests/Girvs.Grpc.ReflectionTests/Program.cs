using System.Reflection;
using Girvs.Grpc;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

var resolver = typeof(GrpcExtensions).GetMethod(
    "GetMapGrpcServiceMethod",
    BindingFlags.NonPublic | BindingFlags.Static);

if (resolver == null)
{
    throw new InvalidOperationException("未找到 MapGrpcService 解析方法。");
}

var resolvedMethod = resolver.Invoke(null, null) as MethodInfo;
if (resolvedMethod == null)
{
    throw new InvalidOperationException("MapGrpcService 解析结果为空。");
}

if (resolvedMethod.Name != nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))
{
    throw new InvalidOperationException($"解析到错误方法：{resolvedMethod.Name}");
}

if (!resolvedMethod.IsGenericMethodDefinition)
{
    throw new InvalidOperationException("MapGrpcService 解析结果不是泛型方法定义。");
}

var parameters = resolvedMethod.GetParameters();
if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IEndpointRouteBuilder))
{
    throw new InvalidOperationException("MapGrpcService 解析结果参数签名错误。");
}

Console.WriteLine("Grpc MapGrpcService 反射解析测试通过。");
