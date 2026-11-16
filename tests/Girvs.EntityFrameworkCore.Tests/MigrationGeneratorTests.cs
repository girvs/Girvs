using Girvs.EntityFrameworkCore.Migrations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

public class MigrationGeneratorTests
{
    [Fact]
    public void 迁移生成器_保留在原组件及原命名空间()
    {
        Assert.Same(typeof(GirvsMigration).Assembly, typeof(GirvsCSharpMigrationsGenerator).Assembly);
        Assert.Same(typeof(GirvsMigration).Assembly, typeof(GirvsCSharpMigrationOperationGenerator).Assembly);
        Assert.Equal("Girvs.EntityFrameworkCore", typeof(GirvsCSharpMigrationsGenerator).Assembly.GetName().Name);
        Assert.Equal("Girvs.EntityFrameworkCore.Migrations", typeof(GirvsCSharpMigrationsGenerator).Namespace);
    }

    [Fact]
    public void 迁移及快照_原组件生成结果可以编译()
    {
        using var context = EfTestScope.CreateContext("Data Source=:memory:");
        var services = new ServiceCollection()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContextDesignTimeServices(context);
        var providerAssembly = typeof(Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions).Assembly;
        var attribute = providerAssembly.GetCustomAttributes(typeof(DesignTimeProviderServicesAttribute), false)
            .Cast<DesignTimeProviderServicesAttribute>().Single();
        ((IDesignTimeServices)Activator.CreateInstance(providerAssembly.GetType(attribute.TypeName)!)!)
            .ConfigureDesignTimeServices(services);
        services.AddSingleton<ICSharpMigrationOperationGenerator, GirvsCSharpMigrationOperationGenerator>();
        services.AddSingleton<IMigrationsCodeGenerator, GirvsCSharpMigrationsGenerator>();
        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<IMigrationsCodeGenerator>();
        var model = context.GetService<IDesignTimeModel>().Model;
        var operations = context.GetService<IMigrationsModelDiffer>().GetDifferences(null, model.GetRelationalModel());
        const string migrationId = "202611160001_Probe";
        var migration = generator.GenerateMigration(GetType().Namespace!, "GeneratedProbe", operations, []);
        var metadata = generator.GenerateMetadata(GetType().Namespace!, typeof(BulkDbContext), "GeneratedProbe", migrationId, model);
#if NET11_0_OR_GREATER
        var snapshot = generator.GenerateSnapshot(GetType().Namespace!, typeof(BulkDbContext), "GeneratedSnapshot", model, migrationId);
        Assert.Contains("LastMigrationId", snapshot);
#else
        var snapshot = generator.GenerateSnapshot(GetType().Namespace!, typeof(BulkDbContext), "GeneratedSnapshot", model);
#endif
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic).Select(assembly => assembly.Location))
            .Where(path => !string.IsNullOrEmpty(path)).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("GeneratedProbe-" + Guid.NewGuid().ToString("N"),
            new[] { migration, metadata, snapshot }.Select(source => CSharpSyntaxTree.ParseText(source)), references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine,
            result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
    }
}
