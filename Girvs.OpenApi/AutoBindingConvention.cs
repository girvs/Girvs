using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Girvs.OpenApi;

public class AutoBindingConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        // 获取 route 模板（如 "api/test/{id}"）
        var routeTemplate = (action.Selectors
            .FirstOrDefault(s => s.AttributeRouteModel != null)?
            .AttributeRouteModel!).Template;

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
                if (!IsRouteParameter(parameter.Name, routeTemplate))
                {
                    parameter.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromQueryAttribute() });
                }
            }
        }
    }

    private static bool IsRouteParameter(string parameterName, string routeTemplate)
    {
        if (string.IsNullOrEmpty(routeTemplate)) return false;

        // 提取所有形如 {param} 的段
        var matches = Regex.Matches(routeTemplate, @"{(\w+)}");
        return matches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Any(name => string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase));
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