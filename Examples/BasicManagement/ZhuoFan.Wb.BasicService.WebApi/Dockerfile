#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Examples/BasicManagement/BasicManagement.WebApi/BasicManagement.WebApi.csproj", "Examples/BasicManagement/BasicManagement.WebApi/"]
RUN dotnet restore "Examples/BasicManagement/BasicManagement.WebApi/BasicManagement.WebApi.csproj"
COPY . .
WORKDIR "/src/Examples/BasicManagement/BasicManagement.WebApi"
RUN dotnet build "BasicManagement.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BasicManagement.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BasicManagement.WebApi.dll"]