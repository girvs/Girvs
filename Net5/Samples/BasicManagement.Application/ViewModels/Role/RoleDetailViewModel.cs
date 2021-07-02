using System;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;

namespace BasicManagement.Application.ViewModels.Role
{
    [AutoMapFrom(typeof(Domain.Models.Role))]
    public class RoleDetailViewModel : IDto
    {
        public Guid Id { get; set; }
        public string Name { get; protected set; }
        public string Desc { get; protected set; }
        public Guid[] UserIds { get; protected set; }
    }
}