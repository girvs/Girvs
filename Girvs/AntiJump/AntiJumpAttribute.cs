namespace Girvs.AntiJump;

/// <summary>
/// 防跳特性，用于标记需要防跳的方法
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AntiJumpAttribute
    : Attribute
{
    /// <summary>
    /// 关键名称，
    /// </summary>
    public string RelationName { get; set; } = "Relation-Request-For-Key";

    /// <summary>
    /// 生成的key
    /// </summary>
    public string GenerateKey { get; set; }

    /// <summary>
    /// 验证的key
    /// </summary>
    public string VerifyKey { get; set; }


    /// <summary>
    /// 防跳的逻辑处理
    /// </summary>
    public AntiJumpLogic AntiJumpLogic { get; set; } = AntiJumpLogic.Generate;

    /// <summary>
    /// 有效时间，单位秒,默认15秒
    /// </summary>
    public int ValidTime { get; set; } = 15;
}

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