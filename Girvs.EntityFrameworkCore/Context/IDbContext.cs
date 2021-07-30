using System.Threading;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Enumerations;

namespace Girvs.EntityFrameworkCore.Context
{
    public interface IDbContext
    {
        void SwitchMasterDataBase();
        string DbConfigName { get; }
        DataConnectionConfig GetDataConnectionConfig();
        DataBaseWriteAndRead ReadAndWriteMode { get; set; }
        string GetDbConnectionString();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        void Dispose();
    }
}