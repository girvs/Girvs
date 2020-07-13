using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Hdyt.SmartProducts.Core;
using Hdyt.SmartProducts.Core.Extensions;
using Hdyt.SmartProducts.Core.Infrastructure.Mapper;
using SmartProducts.Person.Core.Queries;

namespace SmartProducts.Person.WebApi.Model
{
    [AutoMapFrom(typeof(PersonInfoQuery))]
    [AutoMapTo(typeof(PersonInfoQuery))]
    public class RequestPersonQueryModel : QueryModelBase<ResponsePersonMode>
    {
        public string Name { get; set; }
        public string Mobilephone { get; set; }
        public string ConstructionUnitId { get; set; }
    }
}