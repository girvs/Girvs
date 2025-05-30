using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Girvs.OpenApi;

public class AutoBindingConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        foreach (var parameter in action.Parameters)
        {
            // 跳过已明确指定绑定源的参数
            if (parameter.BindingInfo?.BindingSource != null) continue;

            if (IsBodyAllowed(action))
            {
                //处理post put patch请求
                var paramType = parameter.ParameterInfo.ParameterType;
                parameter.BindingInfo = IsSimpleType(paramType)
                    ? BindingInfo.GetBindingInfo([new FromQueryAttribute()])
                    : BindingInfo.GetBindingInfo([new FromBodyAttribute()]);
            }
            else
            {
                // GET请求、DELETE请：所有参数默认为FromQuery
                parameter.BindingInfo = BindingInfo.GetBindingInfo([new FromQueryAttribute()]);
            }
        }
    }

    // private bool IsGetRequest(ActionModel action) => action.Attributes.OfType<HttpGetAttribute>().Any();
    //
    private bool IsBodyAllowed(ActionModel action) =>
        action.Attributes.Any(a =>
            a is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute);

    private bool IsSimpleType(Type type) =>
        type.IsPrimitive
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(TimeSpan)
        || type == typeof(Guid)
        || type.IsEnum
        || Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type));
}