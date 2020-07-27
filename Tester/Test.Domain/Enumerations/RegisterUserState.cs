namespace Test.Domain.Enumerations
{
    /// <summary>
    /// 注册用户状态
    /// </summary>
    public enum RegisterUserState
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal,
        
        /// <summary>
        /// 禁用
        /// </summary>
        Disable,
        
        /// <summary>
        /// 锁定
        /// </summary>
        Locking
    }
}