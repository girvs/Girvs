using System;
using Girvs.Application;
using Girvs.Application.Mapper;

namespace Power.BasicManagement.Application.ViewModels.Role
{
    [AutoMapFrom(typeof(Domain.Models.Role))]
    public class RoleListViewModel : IDto
    {
        public Guid Id { get; set; }
        public string Name { get; protected set; }
        public string Desc { get; protected set; }
    }
}