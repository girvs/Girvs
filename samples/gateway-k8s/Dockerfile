# 宿主 publish + runtime-only 镜像：避免在 docker 内构建全仓库/拉 SDK 镜像。
# 构建前先在宿主执行：
#   dotnet publish samples/gateway-k8s/Gateway/Gateway.csproj -c Release -o samples/gateway-k8s/publish
# 再以本目录（samples/gateway-k8s）为构建上下文：
#   docker build -t girvs-gw-sample:test -f samples/gateway-k8s/Dockerfile samples/gateway-k8s
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ /app/
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
