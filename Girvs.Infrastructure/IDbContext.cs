using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure
{
    public interface IDbContext : IManager
    {
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        void Dispose();
    }
}