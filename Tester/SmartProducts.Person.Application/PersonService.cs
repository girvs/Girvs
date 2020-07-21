using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Managers;
using SmartProducts.Person.Application.Dtos;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Application
{
    public class PersonService : IPersonService
    {
        private readonly IPersonInfoRepository _personInfoRepository;
        private readonly IUnitOfWork _unitOf;
        private readonly ICacheKeyManager<PersonInfoEntity> _cacheKeyManager;
        private readonly IStaticCacheManager _staticCacheManager;

        public PersonService(IPersonInfoRepository personInfoRepository, IUnitOfWork unitOf, ICacheKeyManager<PersonInfoEntity> cacheKeyManager, IStaticCacheManager staticCacheManager)
        {
            _personInfoRepository = personInfoRepository ?? throw new ArgumentNullException(nameof(personInfoRepository));
            _unitOf = unitOf ?? throw new ArgumentNullException(nameof(unitOf));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }
        public async Task<List<PersonListDto>> GetList()
        {
            string[] ss = { };

            _staticCacheManager.Set("trest", "test", 30);

            return new List<PersonListDto>();
        }
    }
}