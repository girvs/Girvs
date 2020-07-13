using Girvs.Domain;
using SmartProducts.Person.Domain.Enumerations;

namespace SmartProducts.Person.Application.Dtos
{
    public class PersonListDto : IDto
    {
        public string Name { get; set; }

        ///<summary>性别</summary>
        public Gender Sex { get; set; }

        ///<summary>政治面貌</summary>
        public PoliticOutlook PoliticOutlook { get; set; }
    }
}