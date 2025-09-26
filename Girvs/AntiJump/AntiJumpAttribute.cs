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

