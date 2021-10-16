#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi/ZhuoFan.Wb.BasicService.WebApi.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Infrastructure/ZhuoFan.Wb.BasicService.Infrastructure.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Infrastructure/"]
COPY ["Girvs/Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj", "Girvs/Girvs.EntityFrameworkCore/"]
COPY ["Girvs/Girvs.DynamicWebApi/Girvs.DynamicWebApi.csproj", "Girvs/Girvs.DynamicWebApi/"]
COPY ["Girvs/Girvs/Girvs.csproj", "Girvs/Girvs/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Domain/ZhuoFan.Wb.BasicService.Domain.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Domain/"]
COPY ["ZhuoFan.Wb.Common/ZhuoFan.Wb.Common.csproj", "ZhuoFan.Wb.Common/"]
COPY ["Girvs/Girvs.EventBus/Girvs.EventBus.csproj", "Girvs/Girvs.EventBus/"]
COPY ["Girvs/Girvs.AuthorizePermission/Girvs.AuthorizePermission.csproj", "Girvs/Girvs.AuthorizePermission/"]
COPY ["Girvs/Girvs.Refit/Girvs.Refit.csproj", "Girvs/Girvs.Refit/"]
COPY ["Girvs/Girvs.Driven/Girvs.Driven.csproj", "Girvs/Girvs.Driven/"]
COPY ["Girvs/Girvs.Cache/Girvs.Cache.csproj", "Girvs/Girvs.Cache/"]
COPY ["Girvs/Girvs.AutoMapper/Girvs.AutoMapper.csproj", "Girvs/Girvs.AutoMapper/"]
COPY ["Girvs/Girvs.Consul/Girvs.Consul.csproj", "Girvs/Girvs.Consul/"]
COPY ["Girvs/Girvs.Grpc/Girvs.Grpc.csproj", "Girvs/Girvs.Grpc/"]
COPY ["Girvs/Girvs.Swagger/Girvs.Swagger.csproj", "Girvs/Girvs.Swagger/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Application/ZhuoFan.Wb.BasicService.Application.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Application/"]
RUN dotnet restore "Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi/ZhuoFan.Wb.BasicService.WebApi.csproj"
COPY . .
WORKDIR "/src/Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi"
RUN dotnet build "ZhuoFan.Wb.BasicService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZhuoFan.Wb.BasicService.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZhuoFan.Wb.BasicService.WebApi.dll"]