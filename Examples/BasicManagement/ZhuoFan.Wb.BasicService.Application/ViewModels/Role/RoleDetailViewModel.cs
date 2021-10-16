using System;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.Role
{
    [AutoMapFrom(typeof(Domain.Models.Role))]
    public class RoleDetailViewModel : IDto
    {
        public RoleDetailViewModel()
        {
            UserIds = Array.Empty<Guid>();
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public Guid[] UserIds { get; set; }
    }
}