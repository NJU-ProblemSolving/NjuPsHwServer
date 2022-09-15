using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.Filteres;

public class AddAuthHeaderOperationFilter : IOperationFilter
{
    public AddAuthHeaderOperationFilter()
    {
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>();

        var allowAnonymousAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AllowAnonymousAttribute>();


        if (authAttributes.Any() && !allowAnonymousAttributes.Any())
        {
            if (!operation.Responses.Any(x => x.Key == "401"))
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            if (!operation.Responses.Any(x => x.Key == "403"))
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

            operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Cookie"
                            },
                            Scheme = "Cookie",
                            Name = "Cookie",
                            In = ParameterLocation.Cookie,

                        },
                        new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "Bearer",
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });
        }
    }
}
