using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;

namespace ServiceDefaults
{
    internal static class OpenApiOptionsExtensions
    {

        //Gắn thông tin chung cho OpenAPI
        public static OpenApiOptions ApplyApiVersionInfo(this OpenApiOptions options, string title, string description)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var versionedDescriptionProvider = context.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
                var apiDescription = versionedDescriptionProvider?.ApiVersionDescriptions
                    .SingleOrDefault(description => description.GroupName == context.DocumentName);
                if (apiDescription is null)
                {
                    return Task.CompletedTask;
                }
                document.Info.Version = apiDescription.ApiVersion.ToString();
                document.Info.Title = title;
                document.Info.Description = BuildDescription(apiDescription, description);
                return Task.CompletedTask;
            });
            return options;
        }

        //Xây dựng chuỗi mô tả thông minh cho tài liệu
        private static string BuildDescription(ApiVersionDescription api, string description)
        {
            var text = new StringBuilder(description);

            if (api.IsDeprecated)
            {
                if (text.Length > 0)
                {
                    if (text[^1] != '.')
                    {
                        text.Append('.');
                    }

                    text.Append(' ');
                }

                text.Append("This API version has been deprecated.");
            }

            if (api.SunsetPolicy is { } policy)
            {
                if (policy.Date is { } when)
                {
                    if (text.Length > 0)
                    {
                        text.Append(' ');
                    }

                    text.Append("The API will be sunset on ")
                        .Append(when.Date.ToShortDateString())
                        .Append('.');
                }

                if (policy.HasLinks)
                {
                    text.AppendLine();

                    var rendered = false;

                    foreach (var link in policy.Links.Where(l => l.Type == "text/html"))
                    {
                        if (!rendered)
                        {
                            text.Append("<h4>Links</h4><ul>");
                            rendered = true;
                        }

                        text.Append("<li><a href=\"");
                        text.Append(link.LinkTarget.OriginalString);
                        text.Append("\">");
                        text.Append(
                            StringSegment.IsNullOrEmpty(link.Title)
                            ? link.LinkTarget.OriginalString
                            : link.Title.ToString());
                        text.Append("</a></li>");
                    }

                    if (rendered)
                    {
                        text.Append("</ul>");
                    }
                }
            }

            return text.ToString();
        }

        // Gắn phương thức bảo mật oauth2 cho api, tạo ổ khoá trên tài liệu
        public static OpenApiOptions ApplySecuritySchemeDefinitions(this OpenApiOptions options)
        {
            options.AddDocumentTransformer<SecuritySchemeDefinitionsTransformer>();
            return options;
        }

        // Đánh dấu các endpoint cần xác thực
        public static OpenApiOptions ApplyAuthorizationChecks(this OpenApiOptions options, string[] scopes)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var authorizationData = metadata.OfType<IAuthorizeData>().ToList();

                if (authorizationData.Count == 0)
                {
                    return Task.CompletedTask;
                }

                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

                var oAuthScheme = new OpenApiSecuritySchemeReference("oauth2", null);
                var configuredScopes = scopes.ToHashSet(StringComparer.Ordinal);
                var requiredScopes = authorizationData
                    .Select(data => data.Policy)
                    .Where(policy => policy is not null && configuredScopes.Contains(policy))
                    .Cast<string>()
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new()
                    {
                        [oAuthScheme] = requiredScopes
                    }
                };

                // var bearerScheme = new OpenApiSecuritySchemeReference("Bearer", null);

                // operation.Security = new List<OpenApiSecurityRequirement>
                // {
                //     new()
                //     {
                //         [bearerScheme] = []
                //     }
                // };

                return Task.CompletedTask;
            });
            return options;
        }

        // Theo dõi Annotaion [Obsolete] để đánh dấu là api đó bị depricate
        public static OpenApiOptions ApplyOperationDeprecatedStatus(this OpenApiOptions options)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                operation.Deprecated = operation.Deprecated || context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<ObsoleteAttribute>()
                    .Any();

                return Task.CompletedTask;
            });
            return options;
        }

        // Chỉnh version của API 
        public static OpenApiOptions ApplyApiVersionDescription(this OpenApiOptions options)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                // Add an example for the API version parameter and remove the default value
                var apiVersionParameter = operation.Parameters?.FirstOrDefault(p => p.Name == "api-version");
                if (apiVersionParameter?.Schema is OpenApiSchema targetSchema)
                {
                    targetSchema.Example = targetSchema.Default;
                    targetSchema.Default = null;
                }
                return Task.CompletedTask;
            });
            return options;
        }


        private class SecuritySchemeDefinitionsTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
        {
            public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
            {
                var identitySection = configuration.GetSection("Identity");
                if (!identitySection.Exists())
                {
                    return Task.CompletedTask;
                }

                var identityUrlExternal = identitySection.GetRequiredValue("Url");
                var scopes = identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value ?? string.Empty);
                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows()
                    {
                        // TODO: Change this to use Authorization Code flow with PKCE
                        // Implicit = new OpenApiOAuthFlow()
                        // {
                        //     AuthorizationUrl = new Uri($"{identityUrlExternal}/oauth2/authorize"),
                        //     TokenUrl = new Uri($"{identityUrlExternal}/oauth2/token"),
                        //     Scopes = scopes,
                        // }
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{identityUrlExternal}/oauth2/authorize"),
                            TokenUrl = new Uri($"{identityUrlExternal}/oauth2/token"),
                            Scopes = scopes
                        
                        }
                    }
                };

                // var securityScheme = new OpenApiSecurityScheme
                // {
                //     Type = SecuritySchemeType.Http,
                //     Scheme = "bearer",
                //     BearerFormat = "JWT",
                //     Description = "Paste the JWT access token returned by the identity service."
                // };

                document.Components ??= new();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes.Add("oauth2", securityScheme);
                return Task.CompletedTask;
            }
        }
    }
}
