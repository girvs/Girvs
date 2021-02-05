using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Configuration;
using Girvs.Domain.Enumerations;

namespace Girvs.Infrastructure
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