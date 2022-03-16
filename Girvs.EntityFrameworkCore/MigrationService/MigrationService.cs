using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.EntityFrameworkCore.MigrationService
{
    [DynamicWebApi]
    [AllowAnonymous]
    public class MigrationService : IMigrationService
    {
        private readonly ILogger<MigrationService> _logger;

        public MigrationService(ILogger<MigrationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{verificationCode}")]
        public async Task<dynamic> InitMigration(string verificationCode)
        {
            var result = new List<string>();

            if (verificationCode.ToMd5() == "zhuofan@168".ToMd5())
            {
                _logger.LogInformation("开始执行数据库还原");
                var typeFinder = new WebAppTypeFinder();
                var dbContexts = typeFinder.FindOfType(typeof
                    (GirvsDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();

                if (!dbContexts.Any())

                    throw new GirvsException("not found dbcontext");


                foreach (var dbContext in dbContexts.Select(dbContextType =>
                    EngineContext.Current.Resolve(dbContextType) as GirvsDbContext))
                {
                    var dbConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
                        .GetDataConnectionConfig(dbContext.GetType());
                    try
                    {
                        dbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                        await dbContext?.Database.MigrateAsync();
                        result.Add($"数据库{dbConfig.Name}：迁移成功！");
                    }
                    catch (Exception e)
                    {
                        result.Add($"数据库{dbConfig.Name}：迁移失败！");
                        _logger.LogError(e, $"数据库{dbConfig.Name}：迁移失败！");
                    }
                }
            }
            else
            {
                result.Add($"较验码输入不正确，请重新输入！");
            }

            return result;
        }
    }
}