namespace Girvs.Infrastructure.CacheRepository.ComponentModel
{
    /// <summary>
    /// 读/写储物柜类型
    /// </summary>
    public enum ReaderWriteLockType
    {
        Read,
        Write,
        UpgradeableRead
    }
}