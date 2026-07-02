using Microsoft.OpenApi.Models;
 
namespace TeamFlow.API.Extensions;
 
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TeamFlow API",
                Version = "v1",
                Description = "API سیستم مدیریت پروژه و همکاری تیمی",
                Contact = new OpenApiContact
                {
                    Name = "TeamFlow",
                    Email = "support@teamflow.io"
                }
            });
 
            // JWT Auth در Swagger
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "توکن JWT را با فرمت: Bearer {token} وارد کنید",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };
 
            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });
 
        return services;
    }
}