namespace Girvs.BusinessBasis.Entities;

public interface IIncludeTamperProof
{
    string DataCheckCode { get; set; }
}

/// <summary>
/// 旧拼写兼容接口，请改用 IIncludeTamperProof
/// </summary>
[Obsolete("拼写修正：请使用 IIncludeTamperProof")]
public interface IIcludeTamperProof : IIncludeTamperProof { }

public static class TamperProofExtensions
{
    public static string GetDataCheckCode(this IIncludeTamperProof obj)
    {
        var hashCodeStr = new StringBuilder();
        var ps = obj.GetType().GetProperties();
        foreach (var propertyInfo in ps)
        {
            var pType = propertyInfo.PropertyType;
            if (
                pType == typeof(string)
                || pType == typeof(int)
                || pType == typeof(double)
                || pType == typeof(float)
                || pType == typeof(bool)
            )
            {
                var pValue = propertyInfo.GetValue(obj);
                hashCodeStr.Append(pValue);
            }
        }

        return hashCodeStr.ToString().ToMd5();
    }

    public static bool DataIsValid(this IIncludeTamperProof obj)
    {
        return obj.GetDataCheckCode() == obj.DataCheckCode;
    }
}
