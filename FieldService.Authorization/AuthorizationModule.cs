using FieldService.Authorization.Data;
using FieldService.Authorization.Data.Repositories;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Mappers;
using FieldService.Authorization.Requirements;
using FieldService.Authorization.Services;
using FieldService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Authorization;

public static class AuthorizationModule
{
    public static IServiceCollection AddAuthorizationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlModule<AuthorizationDbContext>(configuration);
        services.AddScoped<IUserContextRepository, UserContextRepository>();
        services.AddScoped<IUserAuthorizationMapper, UserAuthorizationMapper>();
        services.AddScoped<FieldService.Authorization.Interfaces.IAuthorizationService, AuthorizationService>();
 

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement())
                .Build();

            options.AddPolicy("ActiveUser", policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement()));

            configureAuthorization?.Invoke(options);
        });

        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, RoleAuthorizationHandler>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, FieldServiceAuthorizationPolicyProvider>();

        return services;
    }
    
}
