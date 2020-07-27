using System;
using Girvs.Infrastructure.Repositories;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Infrastructure.DataProvider
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(CmmpDbContext dbContext) : base(dbContext)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
        }
    }
}