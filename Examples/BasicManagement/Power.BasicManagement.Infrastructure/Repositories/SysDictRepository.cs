using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Power.BasicManagement.Infrastructure.Repositories
{
    public class SysDictRepository : Repository<SysDict, int>, ISysDictRepository
    {

        public async Task<SysDict> GetSysDictByCode(string codeType, string code)
        {
            return await DbSet.FirstOrDefaultAsync(x => x.CodeType == codeType && x.Code == code);
        }

        public async Task<SysDict> GetSysDictByName(string codeType, string name)
        {
            return await DbSet.FirstOrDefaultAsync(x => x.CodeType == codeType && x.Name == name);
        }

        public async Task<List<SysDict>> GetSysDictsByCodeType(string codeType)
        {
            return await DbSet.Where(x => x.CodeType == codeType).ToListAsync();
        }
    }
}
