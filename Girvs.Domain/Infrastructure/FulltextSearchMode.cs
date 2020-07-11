namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 全文本搜索模式
    /// </summary>
    public enum FulltextSearchMode
    {
        /// <summary>
        /// 完全匹配（将CONTAINS与prefix_term结合使用）
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// 使用CONTAINS和OR与prefix_term
        /// </summary>
        Or = 5,

        /// <summary>
        /// 使用CONTAINS和AND与prefix_term
        /// </summary>
        And = 10
    }
}