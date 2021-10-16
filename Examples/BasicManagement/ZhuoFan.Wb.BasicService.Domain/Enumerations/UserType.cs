using System;
using System.ComponentModel;

namespace ZhuoFan.Wb.BasicService.Domain.Enumerations
{
    // [Flags]
    // public enum UserType
    // {
    //     /// <summary>
    //     /// 超级管理员
    //     /// </summary>
    //     [Description("管理员用户")] AdminUser = 1,
    //
    //     /// <summary>
    //     /// 考试管理员
    //     /// </summary>
    //     [Description("普通用户")] GeneralUser = 2
    // }


    public enum AuthorizeType

    {
        /// <summary>
        /// 考试管理授权
        /// </summary>
        [Description("管理员授权")] AdminUser,

        /// <summary>
        /// 机构授权
        /// </summary>
        [Description("普通用户授权")] GeneralUser
    }
}