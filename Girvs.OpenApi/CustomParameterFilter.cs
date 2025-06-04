// using Microsoft.AspNetCore.Mvc.ApiExplorer;
// using Microsoft.AspNetCore.Mvc.ModelBinding;
//
// namespace Girvs.OpenApi;
//
// public class CustomParameterFilter : IApiDescriptionProvider
// {
//     public void OnProvidersExecuting(ApiDescriptionProviderContext context)
//     {
//         foreach (var apiDescription in context.Results)
//         {
//             if (apiDescription.HttpMethod == "GET" || apiDescription.HttpMethod == "DELETE")
//             {
//                 foreach (var parameter in apiDescription.ParameterDescriptions)
//                 {
//                     if (parameter.Source == BindingSource.ModelBinding &&
//                         IsSimpleType(parameter.Type))
//                     {
//                         parameter.Source = BindingSource.Query;
//                     }
//                 }
//             }
//         }
//     }
//
//     public void OnProvidersExecuted(ApiDescriptionProviderContext context)
//     {
//     }
//
//     public int Order { get; }
// }