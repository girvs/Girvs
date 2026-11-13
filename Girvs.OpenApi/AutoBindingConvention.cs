using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;

namespace Girvs.OpenApi;

public class AutoBindingConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        // 获取 route 模板（如 "api/test/{id}"）。传统路由没有 AttributeRouteModel，
        // 原写法在 .? 之后用 ! 截断了空传播，会在启动构建应用模型时 NRE。
        var routeTemplate = action
            .Selectors.FirstOrDefault(s => s.AttributeRouteModel != null)?
            .AttributeRouteModel?.Template;

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

        // 用路由模板解析器识别参数，兼容 {id:int}、{id?} 等约束与可选参数；
        // 原正则 \{(\w+)\} 会把它们误判为非路由参数。
        var template = TemplateParser.Parse(routeTemplate);
        return template.Parameters.Any(p =>
            string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
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