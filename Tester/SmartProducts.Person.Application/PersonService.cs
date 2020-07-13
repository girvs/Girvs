using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Caching.RepositoryCache;
using SmartProducts.Person.Application.Dtos;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Application
{
    public class PersonService : IPersonService
    {
        private readonly IPersonInfoRepository _personInfoRepository;
        private readonly IRepositoryCacheManager<PersonInfoEntity> _repositoryCacheManager;

        public PersonService(IPersonInfoRepository personInfoRepository,IRepositoryCacheManager<PersonInfoEntity> repositoryCacheManager)
        {
            _personInfoRepository = personInfoRepository ?? throw new ArgumentNullException(nameof(personInfoRepository));
            _repositoryCacheManager = repositoryCacheManager ?? throw new ArgumentNullException(nameof(repositoryCacheManager));
        }
        public async Task<List<PersonListDto>> GetList()
        {
            string[] ss = { };
            var list = await _repositoryCacheManager.GetAllLinkageAsync(async (fields) => await _personInfoRepository.GetAllAsync(fields), ss);
            return new List<PersonListDto>();
        }
    }
}