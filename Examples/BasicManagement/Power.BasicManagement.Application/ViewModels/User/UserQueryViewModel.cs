using System;
using Girvs.Application;
using Girvs.Application.Mapper;
using Power.BasicManagement.Domain.Enumerations;
using Power.BasicManagement.Domain.Queries;

namespace Power.BasicManagement.Application.ViewModels.User
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
    }
}