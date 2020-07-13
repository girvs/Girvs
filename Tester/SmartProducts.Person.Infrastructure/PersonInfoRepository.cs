using System;
using System.Threading.Tasks;
using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Infrastructure
{
    public class PersonInfoRepository : BaseActionRepository<PersonInfoEntity>, IPersonInfoRepository
    {
        private readonly PersonDbContext dbContext;

        public PersonInfoRepository(PersonDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            CurrentDataTable = dbContext.PersonInfos;
        }

        public async Task<PersonInfoEntity> GetPersonInfoByCardAsync(string card)
        {
            return await CurrentDataTable.SingleOrDefaultAsync(x => x.WorkCard == card);
        }
    }
}
