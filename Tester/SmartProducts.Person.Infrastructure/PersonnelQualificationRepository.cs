using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Infrastructure.Repositories;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Infrastructure
{
    public class PersonnelQualificationRepository : Repository<PersonnelQualificationEntity>, IPersonnelQualificationRepository
    {
        private readonly PersonDbContext _dbContext;

        public PersonnelQualificationRepository(PersonDbContext dbContext) : base(dbContext)
        {
            this._dbContext = dbContext;
            DbSet = dbContext.PersonnelQualifications;
        }

        public async Task<List<PersonnelQualificationEntity>> GetListByPersonInfo(Guid PersonInfoId)
        {
            throw new Exception();
        }
    }
}
