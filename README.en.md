# Girvs Rapid Development Framework

<div align="center">

[![GitHub](https://img.shields.io/github/license/girvs/Girvs)](https://github.com/girvs/Girvs/blob/master/LICENSE)
[![Version](https://img.shields.io/badge/release-11.0.0--preview1.9-blue.svg)](https://github.com/girvs/Girvs/tags)
[![.NET](https://img.shields.io/badge/.NET-8.0%20|%209.0%20|%2010.0%20|%2011.0-purple.svg)](https://dotnet.microsoft.com/)

</div>

## 📖 Introduction

Girvs is an enterprise-grade rapid development framework based on .NET, designed to provide a complete microservice development solution. The framework adopts a modular design and integrates common infrastructure components, helping developers quickly build high-performance, scalable distributed applications.

> Most design documents and guides in this repository (such as the upgrade guide) are written in Chinese.

## ✨ Key Features

- 🎯 **Modular Architecture** - Loosely coupled modules, import on demand
- 🚀 **Multi-targeting** - Common libraries support .NET 8.0, 9.0, 10.0 and 11.0 simultaneously; see the version support table for details
- 🔧 **Out of the Box** - Common components integrated, less repeated work
- 📦 **NuGet Packages** - All modules are available on NuGet
- 🔐 **Secure & Reliable** - Built-in permission authorization, data validation and other security mechanisms
- 🌐 **Microservice Friendly** - Service registration/discovery, unified gateway, distributed event bus
- 📊 **Observability** - Comprehensive logging and monitoring support

## 🏗️ Project Structure

```
Girvs/
├── Girvs                          # Core framework module
├── Girvs.ServiceGovernance        # Unified service governance module (net10.0/net11.0)
├── Girvs.Aspire.Hosting           # .NET Aspire AppHost orchestration module (net8.0 to net11.0)
├── Girvs.AuthorizePermission      # Permission authorization module
├── Girvs.AutoMapper               # Object mapping module
├── Girvs.Cache                    # Cache module (Redis/SqlServer)
├── Girvs.CodeGenerator            # Code generator module
├── Girvs.Consul                   # Service registration & discovery module
├── Girvs.Driven                   # Domain-Driven Design support module
├── Girvs.DynamicWebApi            # Dynamic Web API module
├── Girvs.EntityFrameworkCore      # EF Core data access module
├── Girvs.EventBus                 # Event bus module (based on CAP)
├── Girvs.Gateway                  # Gateway module (YARP service-discovery routing, net10.0/net11.0)
├── Girvs.Grpc                     # gRPC service module
├── Girvs.OpenApi                  # OpenAPI documentation module
├── Girvs.Quartz                   # Scheduled jobs module
├── Girvs.Refit                    # HTTP client module
└── Girvs.SignalR                  # Real-time communication module
```

## 📦 Core Modules

### Girvs (Core Framework)
The core base library of the framework, providing fundamental abstractions and utilities.

**Main features:**
- Logging (based on Serilog)
- Configuration management
- Dependency injection extensions
- Common utilities

**Logging sinks:**
- Console output
- File storage (when no business `Serilog.json` is provided, the fallback logger writes to both Console and an hourly rolling file)
- Elasticsearch integration

### Girvs.EntityFrameworkCore (Data Access)
Data access module based on Entity Framework Core.

**Supported databases (by target framework):**
- SQL Server (all targets)
- MySQL (Pomelo on net8/net9, Microting.EntityFrameworkCore.MySql on net10/net11)
- SQLite, InMemory (net8)
- Oracle (net8)

**Main features:**
- Repository pattern
- Unit of Work
- Database migration management
- Lazy loading support (Proxies)
- Table structure management

### Girvs.Cache (Caching)
A unified interface over multiple cache implementations.

**Supported caches:**
- Redis (based on StackExchange.Redis)
- SQL Server
- In-memory cache

**Main features:**
- Unified cache interface
- Cache key management
- Expiration policy configuration
- Async operation support

### Girvs.EventBus (Event Bus)
Distributed event bus implementation based on DotNetCore.CAP.

**Supported message brokers:**
- RabbitMQ
- Kafka
- Redis Streams

**Supported message storage:**
- MySQL
- SQL Server
- SQLite

**Main features:**
- Publish/subscribe pattern
- Message persistence
- Failure retry mechanism
- Visual management dashboard
- Automatic identity propagation (the publisher serializes the current identity into the `girvs-identity` header; the consumer restores the user and tenant automatically)
- Per-message scope bridging (`Handle` runs in a dedicated scope for the whole message; constructor injection and `EngineContext.Resolve` hit the same `DbContext` instance)
- Fail-fast configuration validation at startup (unknown persistence/transport types fail immediately)

### Girvs.Gateway (Gateway, net10.0/net11.0)
A dynamic gateway based on YARP, generating routes and clusters from service discovery results.

**Supported discovery sources:**
- Aspire AppHost (local development)
- Consul (traditional registry)
- Kubernetes (watch Service, routes refresh within seconds)

**Main features:**
- Convention-based route generation (`/{ServiceName}/{**catch-all}`, prefix stripped on forwarding)
- Hot route updates (change-token driven, no restart required)
- Optional Redis flow protection (strict linear-flow tickets)
- Configurable forwarding idle timeout (default 5 minutes)

See the [Girvs.Gateway README](Girvs.Gateway/README.md) (Chinese) for details.

### Girvs.DynamicWebApi (Dynamic API)
Automatically converts application services into RESTful APIs.

**Main features:**
- Automatic route generation
- RESTful style APIs
- Model state validation
- Mini API support

### Girvs.AuthorizePermission (Authorization)
A complete permission authorization solution.

**Main features:**
- Custom authentication scheme
- Role-based permission control
- Data rule filtering
- Method-level permission control
- Permission cache management

### Girvs.Grpc (gRPC Services)
gRPC service support module.

**Main features:**
- gRPC service configuration
- Exception interceptors
- Automatic service registration

### Girvs.Quartz (Scheduled Jobs)
Job scheduling based on Quartz.NET.

**Main features:**
- Job scheduling
- Cron expression support
- Scope-bridged job factory (a dedicated DI scope per job trigger eliminates root-container `DbContext` sharing across threads)
- Automatic activation of unregistered jobs (falls back to `ActivatorUtilities` construction)
- Background job hosting

### Girvs.Consul (Service Registration & Discovery)
Service registration and discovery based on Consul.

**Main features:**
- Service registration
- Health checks
- Service discovery
- Configuration center

### Girvs.ServiceGovernance (Unified Service Governance, net10.0/net11.0)
Provides health checks, service discovery and observability; Aspire and Consul discovery modes are mutually exclusive by configuration.

**Main features:**
- OpenTelemetry observability (traces/metrics/logs, bridged via the Serilog OTLP sink)
- `/health` and `/alive` health check endpoints
- Aspire service discovery and standard HttpClient resilience policies
- Automatic mapping of Aspire-injected connection strings into Girvs module configurations

### Girvs.Aspire.Hosting (.NET Aspire AppHost Orchestration, net8.0 to net11.0)
Referenced by business AppHost projects; creates/reuses resources and wires them up according to the `[DependsOn]` declarations of each service's root module.

**Main features:**
- One-line onboarding via `AddGirvsProject`, automatically orchestrating Redis/RabbitMQ/MySql/SqlServer resources
- Shared resources across services, dedicated database per service
- Custom module resource contributor extension point

See the [Aspire guide](docs/aspire/apphost-guide.md) (Chinese) and the architecture diagram `Architect.png`.

### Girvs.SignalR (Real-time Communication)
Real-time communication module based on SignalR.

**Main features:**
- WebSocket support
- JWT authentication integration
- Endpoint routing configuration

### Girvs.Refit (HTTP Client)
Declarative HTTP client wrapper.

**Main features:**
- Refit integration
- Automatic service registration
- Custom HttpClient handlers

### Girvs.AutoMapper (Object Mapping)
Object mapping module based on AutoMapper.

**Main features:**
- Automatic profile discovery
- Ordered mapping configuration
- Dependency injection integration

### Girvs.Driven (Domain-Driven Design)
DDD (Domain-Driven Design) support module.

**Main features:**
- Commands
- Queries
- Events
- Notifications
- MediatR integration
- Validation behaviors
- Caching behaviors

### Girvs.OpenApi (API Documentation)
OpenAPI 3.0 document generation.

**Main features:**
- Swagger UI integration
- Bearer token authentication
- Custom parameter filtering
- Automatic binding conventions

### Girvs.CodeGenerator (Code Generator)
Code scaffolding tool.

**Main features:**
- Code template management
- Code generation services
- Custom template support

## 🚀 Quick Start

### Prerequisites

- Building this repository from source requires .NET SDK 11.0.100 (see `global.json`); when consuming the libraries, install the runtime matching your target framework
- Visual Studio 2022 or JetBrains Rider
- (Optional) Docker Desktop

### Installation

Install the modules you need from the NuGet package manager. The latest preview is `11.0.0-preview1.9`. For net10/net11 dependency details and verification, see the [.NET 11 upgrade guide](docs/dotnet11-upgrade.md) (Chinese).

```bash
# Core framework
dotnet add package Girvs --version 11.0.0-preview1.9

# EntityFrameworkCore module
dotnet add package Girvs.EntityFrameworkCore --version 11.0.0-preview1.9

# Cache module
dotnet add package Girvs.Cache --version 11.0.0-preview1.9

# Event bus
dotnet add package Girvs.EventBus --version 11.0.0-preview1.9
```

### Basic Usage

#### 1. Program entry and Startup

```csharp
// Program.cs
var app = GirvsHostBuilderManager.CreateGirvsWebApplication(args);
app.Run();
```

```csharp
public class Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    : IGirvsStartup
{
    public IConfiguration Configuration { get; } = configuration;
    public IWebHostEnvironment WebHostEnvironment { get; } = webHostEnvironment;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithAuthorizePermissionFilter(options =>
            options.Filters.Add<GirvsModelStateInvalidFilter>()
        );
        services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseGirvsExceptionHandler();
        app.UseRouting();
        app.ConfigureRequestPipeline(env);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.ConfigureEndpointRouteBuilder();
        });
    }
}
```

#### 2. Data access example

```csharp
// Define the entity
public class Product : AggregateRoot<Guid>,
    IIncludeCreatorId<Guid>,
    IIncludeCreatorName,
    IIncludeMultiTenant<Guid>,
    IIncludeCreateTime,
    ITenantShardingTable, // automatic tenant-based table sharding
    IIncludeMultiTenantName // multi-tenant fields
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Use the repository
public class ProductService
{
    private readonly IRepository<Product> _productRepository;

    public ProductService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product> GetByIdAsync(Guid id)
    {
        return await _productRepository.GetByIdAsync(id);
    }
}
```

#### 3. Event bus example

```csharp
// Define the event
public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

// Publish the event
public class OrderService
{
    private readonly IEventBus _eventBus;

    public async Task CreateOrderAsync(Order order)
    {
        // Save the order...

        // Publish the event
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            Amount = order.Amount
        });
    }
}

// Subscribe: derive from GirvsIntegrationEventHandler<T>; Handle automatically gets
// identity restoration and a dedicated scope. Put the consume logic directly in Handle;
// do NOT start fire-and-forget tasks that outlive the message.
public class OrderCreatedEventHandler : GirvsIntegrationEventHandler<OrderCreatedEvent>
{
    [CapSubscribe(nameof(OrderCreatedEvent))]
    public override Task Handle(
        OrderCreatedEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        // Handle the order-created event
        Console.WriteLine($"Order created: {@event.OrderId}");
        return Task.CompletedTask;
    }
}
```

> The subscriber requires `using DotNetCore.CAP;` (`CapSubscribe`, `FromCap`) and `using Girvs.EventBus;`.

## 📝 Configuration

Infrastructure modules declare resources under the top-level `Resources` section and reference them with a `ConnectionRef` string array: index 0 is the resource-key format string, and the remaining entries are `string.Format` arguments.

```json
{
  "Resources": {
    "platform-redis": {
      "Type": "redis",
      "Settings": { "Endpoints": "redis:6379,ssl=False,defaultdatabase={0}" }
    }
  },
  "ModuleConfigurations": {
    "CacheConfig": {
      "DistributedCacheConfig": {
        "Enabled": true,
        "ConnectionRef": ["platform-redis", "0"]
      }
    }
  }
}
```

Database, event bus, SignalR and other modules use the same resource reference model. The old structured connection configuration has been removed; see the [framework upgrade guide](框架升级说明.md) (Chinese) for the full migration instructions.

## 🔧 Version Support

| Module | .NET 8.0 | .NET 9.0 | .NET 10.0 | .NET 11.0 |
|------|----------|----------|-----------|-----------|
| Common libraries (incl. Girvs.Aspire.Hosting, 14 total) | ✅ | ✅ | ✅ | ✅ |
| Girvs.OpenApi | — | ✅ | ✅ | ✅ |
| Girvs.Gateway, Girvs.ServiceGovernance | — | — | ✅ | ✅ |

Latest release: **11.0.0-preview1.9** (published by the tag-triggered pipeline; see the [upgrade guide](docs/dotnet11-upgrade.md) for the release process, in Chinese). The development baseline version is 10.0.0-rc.4.0.3 from `Directory.Build.props`. All 17 libraries include the `net11.0` target. Behavior changes in the `v11.0.0-preview1.x` series are documented in the [framework upgrade guide](框架升级说明.md) (Chinese).

## 📚 Learning Resources

### Open-source projects we learned from

1. **nopCommerce** - https://github.com/nopSolutions/nopCommerce
   - E-commerce best practices
   - Plugin-based architecture

2. **ChristDDD** - https://github.com/anjoy8/ChristDDD
   - DDD implementation
   - CQRS pattern

3. **eShopOnContainers** - https://github.com/dotnet-architecture/eShopOnContainers
   - Microservice architecture reference
   - Containerized deployment

## 🤝 Contributing

Issues and pull requests are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the [Apache License 2.0](https://github.com/girvs/Girvs/blob/master/LICENSE).

## 🔗 Links

- **GitHub**: https://github.com/girvs/Girvs
- **Issues**: https://github.com/girvs/Girvs/issues
- **NuGet packages**: https://www.nuget.org/packages?q=Girvs

## 👨‍💻 Author

**kicck**

## 📮 Contact

For questions or suggestions, feel free to reach out via GitHub Issues.

---

<div align="center">

**⭐ If this project helps you, please give it a star! ⭐**

</div>
