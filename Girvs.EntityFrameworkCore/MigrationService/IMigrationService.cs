namespace Girvs.EntityFrameworkCore.MigrationService;

public interface IMigrationService : IAppWebApiService
{
    Task<dynamic> InitMigration(string verificationCode);
}