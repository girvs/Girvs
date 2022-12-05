namespace Girvs.BusinessBasis.Entities;

public interface IIcludeTamperProof
{
    string DataCheckCode { get; set; }
}

public static class TamperProofExtensions
{
    public static string GetDataCheckCode(this IIcludeTamperProof obj)
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

    public static bool DataIsValid(this IIcludeTamperProof obj)
    {
        return obj.GetDataCheckCode() == obj.DataCheckCode;
    }
}