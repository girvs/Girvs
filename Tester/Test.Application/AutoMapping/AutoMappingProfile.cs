using AutoMapper;
using Girvs.Domain.Infrastructure;

namespace Test.Application.AutoMapping
{
    public class AutoMappingProfile : Profile, IOrderedMapperProfile
    {
        public AutoMappingProfile()
        {
            //CreateMap<Cmmp.Domain.Entities.User, RequestAddOrUpdateUserModel>();
            //CreateMap<RequestAddOrUpdateUserModel, Cmmp.Domain.Entities.User>();
        }

        public int Order { get; } = 3;
    }
}