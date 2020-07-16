using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Caching.RepositoryCache;
using Girvs.Domain.Managers;
using SmartProducts.Person.Application.Dtos;
using SmartProducts.Person.Domain.Entities;
using SmartProducts.Person.Domain.Repositories;

namespace SmartProducts.Person.Application
{
    public class PersonService : IPersonService
    {
        private readonly IPersonInfoRepository _personInfoRepository;
        private readonly IRepositoryCacheManager<PersonInfoEntity> _repositoryCacheManager;
        private readonly IUnitOfWork _unitOf;

        public PersonService(IPersonInfoRepository personInfoRepository,IRepositoryCacheManager<PersonInfoEntity> repositoryCacheManager,IUnitOfWork unitOf)
        {
            _personInfoRepository = personInfoRepository ?? throw new ArgumentNullException(nameof(personInfoRepository));
            _repositoryCacheManager = repositoryCacheManager ?? throw new ArgumentNullException(nameof(repositoryCacheManager));
            _unitOf = unitOf ?? throw new ArgumentNullException(nameof(unitOf));
        }
        public async Task<List<PersonListDto>> GetList()
        {
            string[] ss = { };
            var list = await _repositoryCacheManager.GetAllLinkageAsync(async (fields) => await _personInfoRepository.GetAllAsync(fields), ss);
            return new List<PersonListDto>();
        }
    }
}