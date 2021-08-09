#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi/ZhuoFan.Wb.BasicService.WebApi.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Infrastructure/ZhuoFan.Wb.BasicService.Infrastructure.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Infrastructure/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Domain/ZhuoFan.Wb.BasicService.Domain.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Domain/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicService.Application/ZhuoFan.Wb.BasicService.Application.csproj", "Services/BasicManagement/ZhuoFan.Wb.BasicService.Application/"]
COPY ["ZhuoFan.Wb.EventBus/ZhuoFan.Wb.EventBus.csproj", "ZhuoFan.Wb.EventBus/"]
COPY ["Services/BasicManagement/ZhuoFan.Wb.BasicManagement.sln", "Services/BasicManagement/"]

RUN dotnet restore "Services/BasicManagement/ZhuoFan.Wb.BasicManagement.sln"
COPY . .
WORKDIR "/src/Services/BasicManagement/ZhuoFan.Wb.BasicService.WebApi"
RUN dotnet build "ZhuoFan.Wb.BasicService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZhuoFan.Wb.BasicService.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZhuoFan.Wb.BasicService.WebApi.dll"]