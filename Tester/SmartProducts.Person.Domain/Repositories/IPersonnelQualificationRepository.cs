using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.IRepositories;
using SmartProducts.Person.Domain.Entities;

namespace SmartProducts.Person.Domain.Repositories
{
    public interface IPersonnelQualificationRepository : IRepository<PersonnelQualificationEntity>
    {
        Task<List<PersonnelQualificationEntity>> GetListByPersonInfo(Guid PersonInfoId);
    }
}
