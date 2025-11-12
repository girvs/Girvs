using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;

#if NET10_0_OR_GREATER
using Microsoft.OpenApi;

#else
using Microsoft.OpenApi.Models;
#endif

namespace Girvs.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        ServicesManager(document);
        TransformerServer(document);
        await TransformerBearer(document);
    }

    private void ServicesManager(OpenApiDocument document)
    {
        document.Servers.Clear();
        var serviceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
        document.Servers.Add(new OpenApiServer() {Url = "/", Description = "默认访问"});
        document.Servers.Add(new OpenApiServer() {Url = "/" + serviceName, Description = "网关访问"});
    }

    private async Task TransformerBearer(OpenApiDocument document)
    {
#if NET10_0_OR_GREATER

        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // Add the security scheme at the document level
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            // Apply it as a requirement for all operations
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
#else
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any())
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer",
                                    Type = ReferenceType.SecurityScheme
                                }
                            }
                        ] = []
                    }
                );
            }
        }
#endif
    }

    private static void TransformerServer(OpenApiDocument document)
    {
        // if (document.Servers.Count > 0)
        // {
        //     var defaultServer = document.Servers[0];
        //     defaultServer.Description = "默认访问";
        //     defaultServer.Url = "/";
        // }
        //
        // var config = EngineContext.Current.GetAppModuleConfig<OpenApiConfig>();
        // if (config.ArchitectureType == ArchitectureType.Microservices)
        // {
        //     var serviceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
        //     var server = new OpenApiServer { Description = "通过网关进行访问", Url = "/" + serviceName };
        //     document.Servers.Add(server);
        // }
    }
}