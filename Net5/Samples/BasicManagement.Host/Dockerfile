#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Samples/BasicManagement.Host/BasicManagement.Host.csproj", "Samples/BasicManagement.Host/"]
COPY ["Samples/BasicManagement.Application/BasicManagement.Application.csproj", "Samples/BasicManagement.Application/"]
COPY ["Samples/BasicManagement.Domain/BasicManagement.Domain.csproj", "Samples/BasicManagement.Domain/"]
COPY ["Girvs.Cache/Girvs.Cache.csproj", "Girvs.Cache/"]
COPY ["Girvs/Girvs.csproj", "Girvs/"]
COPY ["Girvs.AuthorizePermission/Girvs.AuthorizePermission.csproj", "Girvs.AuthorizePermission/"]
COPY ["Girvs.Driven/Girvs.Driven.csproj", "Girvs.Driven/"]
COPY ["Girvs.AutoMapper/Girvs.AutoMapper.csproj", "Girvs.AutoMapper/"]
COPY ["Girvs.DynamicWebApi/Girvs.DynamicWebApi.csproj", "Girvs.DynamicWebApi/"]
COPY ["Girvs.Swagger/Girvs.Swagger.csproj", "Girvs.Swagger/"]
COPY ["Samples/BasicManagement.Infrastructure/BasicManagement.Infrastructure.csproj", "Samples/BasicManagement.Infrastructure/"]
COPY ["Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj", "Girvs.EntityFrameworkCore/"]
RUN dotnet restore "Samples/BasicManagement.Host/BasicManagement.Host.csproj"
COPY . .
WORKDIR "/src/Samples/BasicManagement.Host"
RUN dotnet build "BasicManagement.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BasicManagement.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BasicManagement.Host.dll"]