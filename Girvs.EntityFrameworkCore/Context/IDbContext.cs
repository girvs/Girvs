using System.Threading;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Enumerations;

namespace Girvs.EntityFrameworkCore.Context
{
    public interface IDbContext
    {
        void SwitchReadWriteDataBase(DataBaseWriteAndRead dataBaseWriteAndRead);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        void Dispose();
    }
}