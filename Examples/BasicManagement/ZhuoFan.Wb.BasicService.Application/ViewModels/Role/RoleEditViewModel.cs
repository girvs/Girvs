using System;
using System.ComponentModel.DataAnnotations;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.Role
{
    [AutoMapFrom(typeof(Domain.Models.Role))]
    public class RoleEditViewModel : IDto
    {
        public RoleEditViewModel()
        {
            UserIds = Array.Empty<Guid>();
        }

        public Guid? Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        public string Desc { get; set; }
        public Guid[] UserIds { get; set; }
    }
}