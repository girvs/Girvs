using System;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Queries;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    [AutoMapFrom(typeof(UserQuery))]
    [AutoMapTo(typeof(UserQuery))]
    public class UserQueryViewModel : QueryDtoBase<UserQueryListViewModel>
    {
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 用户登陆名称
        /// </summary>
        public string UserAccount { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public DataState? DataState { get; set; }
    }

    [AutoMapFrom(typeof(Domain.Models.User))]
    public class UserQueryListViewModel : IDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 用户登陆名称
        /// </summary>
        public string UserAccount { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public DataState State { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
    }
}