using System;
using System.Linq.Expressions;
using Girvs.Domain.Extensions;
using Girvs.Domain.Managers;
using SmartProducts.Person.Domain.Entities;

namespace SmartProducts.Person.Domain.Queries
{
    public class PersonInfoQuery : QueryBase<PersonInfoEntity>
    {
        public string Name { get; set; }
        public string Mobilephone { get; set; }

        public string ConstructionUnitId { get; set; }
        public override Expression<Func<PersonInfoEntity, bool>> GetQueryWhere()
        {
            Expression<Func<PersonInfoEntity, bool>> ex = x => true;

            if (!string.IsNullOrEmpty(Name))
            {
                ex = ex.And(x => x.Name.Contains(Name));
            }

            if (!string.IsNullOrEmpty(Mobilephone))
            {
                ex = ex.And(x => x.Mobilephone == Mobilephone);
            }

            if (!string.IsNullOrEmpty(ConstructionUnitId))
            {
                ex = ex.And(x => x.ConstructionUnitId == ConstructionUnitId);
            }

            return ex;
        }
    }
}
