using System.Threading.Tasks;
using Girvs.DynamicWebApi;

namespace Girvs.EntityFrameworkCore.MigrationService
{
    public interface IMigrationService : IAppWebApiService
    {
        Task<dynamic> InitMigration(string verificationCode);
    }
}