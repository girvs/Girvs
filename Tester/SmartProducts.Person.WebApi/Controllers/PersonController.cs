using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartProducts.Person.Application;
using SmartProducts.Person.Application.Dtos;

namespace SmartProducts.Person.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IPersonService _personService;

        public PersonController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpGet]
        public async Task<List<PersonListDto>> GetAll()
        {
            return await _personService.GetList();
        }
    }
}