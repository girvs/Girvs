using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Power.BasicManagement.Infrastructure;

namespace Power.BasicManagement.WebApi.App_Data
{
    public class DbContextFactory : IDesignTimeDbContextFactory<BasicManagementDbContext>
    {
        public BasicManagementDbContext CreateDbContext(string[] args)
        {

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BasicManagementDbContext>();
            optionsBuilder.UseMySql(configuration["Girvs:DataConnectionString"]);

            return new BasicManagementDbContext(optionsBuilder.Options);
        }
    }
}