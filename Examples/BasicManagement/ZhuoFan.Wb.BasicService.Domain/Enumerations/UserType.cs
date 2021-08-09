using System.ComponentModel;

namespace ZhuoFan.Wb.BasicService.Domain.Enumerations
{
    public enum UserType
    {
        /// <summary>
        /// 超级管理员
        /// </summary>
        [Description("超级管理员")]
        SuperAdmin,
        
        /// <summary>
        /// 考试管理员
        /// </summary>
        [Description("考试管理员")]
        ExamAdmin,
        
        /// <summary>
        /// 机构管理员
        /// </summary>
        [Description("机构用户")]
        OrganizationUser,

        /// <summary>
        /// 单位管理员
        /// </summary>
        [Description("单位用户")]
        UnitUser,
        
        /// <summary>
        /// 授权用户
        /// </summary>
        [Description("单位管理员")]
        AuthorizeUser
    }


    public enum AuthorizeType
    
    {
        /// <summary>
        /// 考试管理授权
        /// </summary>
        [Description("考试管理授权")]
        Exam,
        
        /// <summary>
        /// 机构授权
        /// </summary>
        [Description("机构授权")]
        Organization,

        /// <summary>
        /// 单位授权
        /// </summary>
        [Description("单位授权")]
        Unit,
    }
}
