using Girvs.Application;
using SmartProducts.Person.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartProducts.Person.Application
{
    public interface IPersonService:IService
    {
        Task<List<PersonListDto>> GetList();
    }
}