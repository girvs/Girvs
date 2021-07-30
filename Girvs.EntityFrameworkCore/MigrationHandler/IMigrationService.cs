using System.Threading.Tasks;
using Girvs.DynamicWebApi;

namespace Girvs.EntityFrameworkCore.MigrationHandler
{
    public interface IMigrationService : IAppWebApiService
    {
        Task<dynamic> InitMigration(string verificationCode);
    }
}