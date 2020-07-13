using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Infrastructure.Repositories;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Infrastructure
{
    public class PersonnelQualificationRepository : BaseActionRepository<PersonnelQualificationEntity>, IPersonnelQualificationRepository
    {
        private readonly PersonDbContext dbContext;

        public PersonnelQualificationRepository(PersonDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
            CurrentDataTable = dbContext.PersonnelQualifications;
        }

        public async Task<List<PersonnelQualificationEntity>> GetListByPersonInfo(Guid PersonInfoId)
        {
            throw new Exception();
        }
    }
}
