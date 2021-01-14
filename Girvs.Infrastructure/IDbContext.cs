using System;
using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure
{
    public interface IDbContext : IManager, IDisposable
    {
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync();
    }
}