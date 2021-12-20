namespace Girvs.EntityFrameworkCore.Enumerations
{
    /// <summary>
    /// 数据分割模式
    /// </summary>
    public enum MultiTenantDataSeparateModel
    {
        /// <summary>
        /// 不进行分
        /// </summary>
        None,

        /// <summary>
        /// 根据数据库进行分隔
        /// </summary>
        // DataBase,

        /// <summary>
        /// 根据数据表进行分隔
        /// </summary>
        DataTable
    }
}