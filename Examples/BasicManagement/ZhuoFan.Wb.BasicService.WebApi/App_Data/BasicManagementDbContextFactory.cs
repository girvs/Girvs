using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ZhuoFan.Wb.BasicService.Infrastructure;

namespace ZhuoFan.Wb.BasicService.WebApi.App_Data
{
#if DEBUG
    
    public class AuditLogDbContextFactory : IDesignTimeDbContextFactory<BasicManagementDbContext>
    {
        public BasicManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BasicManagementDbContext>();
            optionsBuilder.UseMySql("Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;",
                new MySqlServerVersion(new Version(8, 0, 25)));
            return new BasicManagementDbContext(optionsBuilder.Options);
        }
    }
    
#endif

}
