namespace Girvs.EntityFrameworkCore.MigrationService;

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

            if (dbContexts.Count == 0)

                throw new GirvsException("not found dbcontext");


            foreach (var dbContext in dbContexts.Select(dbContextType =>
                         EngineContext.Current.Resolve(dbContextType) as GirvsDbContext))
            {
                var dbConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
                    .GetDataConnectionConfig(dbContext.GetType());
                try
                {
                    dbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                    await dbContext.Database.MigrateAsync();
                    result.Add($"数据库{dbConfig.Name}：迁移成功！");
                }
                catch (Exception e)
                {
                    result.Add($"数据库{dbConfig.Name}：迁移失败！");
                    // 使用结构化模板，避免关闭日志级别时仍插值生成字符串。
                    _logger.LogError(e, "数据库{DatabaseName}：迁移失败！", dbConfig.Name);
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