namespace Girvs.AntiJump;

/// <summary>
/// 防跳逻辑
/// </summary>
[Flags]
public enum AntiJumpLogic : long
{
    /// <summary>
    /// 代表是需要生成防跳的key，然后返回给前端，前端需要将这个key存储起来，然后在提交的时候带上这个key，然后后台会验证这个key是否正确
    /// </summary>
    Generate = 1,

    /// <summary>
    /// 代表是需要验证防跳的key，后台会验证这个key是否正确
    /// </summary>
    Verify = 2,

    /// <summary>
    /// 验证的同时还要生成
    /// </summary>
    All = Generate | Verify
}