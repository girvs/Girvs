using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Test.Infrastructure;

namespace Test.GrpcServiceWebHost.App_Data
{
    public class DbContextFactory : IDesignTimeDbContextFactory<CmmpDbContext>
    {
        public CmmpDbContext CreateDbContext(string[] args)
        {

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<CmmpDbContext>();
            optionsBuilder.UseOracle(configuration["Girvs:DataConnectionString"]);

            return new CmmpDbContext(optionsBuilder.Options);
        }
    }
}