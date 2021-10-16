using AutoMapper;
using Girvs.AuthorizePermission;
using Girvs.AutoMapper;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Application.AutoMapperProfiles
{
    public class AuthorizeProfile : Profile, IOrderedMapperProfile
    {
        public AuthorizeProfile()
        {
            CreateMap<AuthorizeDataRuleModel, UserRule>();
            CreateMap<UserRule, AuthorizeDataRuleModel>();


            CreateMap<AuthorizePermissionModel, BasalPermission>();
            CreateMap<BasalPermission, AuthorizePermissionModel>();
        }

        public int Order { get; } = 12;
    }
}