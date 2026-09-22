using FieldService.Authentication.Data;
using FieldService.Authentication.Data.Repositories;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Mappers;
using FieldService.Authentication.Services;
using FieldService.Authentication.Types;
using FieldService.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace FieldService.Authentication;

public static class AuthenticationModule
{
    public static IServiceCollection AddAuthenticationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var authenticationOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
            ?? throw new InvalidOperationException("Authentication configuration is missing. " +
                                                   "Configure 'Authentication' section in appsettings.");

        services.AddSqlModule<AuthenticationDbContext>(configuration);
        services.AddHttpContextAccessor();
        services.AddSingleton(authenticationOptions);
        services.AddScoped<IIdentityProvider, AzureEntraIdentityProvider>();
        services.AddSingleton<ISessionMapper, SessionMapper>();
        services.AddSingleton<IUserAuthenticationMapper, UserAuthenticationMapper>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IUserAuthenticationRepository, UserAuthenticationRepository>();
        services.AddScoped<IInternalUserAuthenticationService, InternalUserAuthenticationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISessionPersistenceService, SessionCleanupService>();
        services.AddScoped<ISessionManager, SessionManager>();
        if (environment?.IsDevelopment() == true)
        {
            services.AddScoped<IUserIdentityResolver, DevelopmentEnvironmentUserIdentityResolver>();
        }
        else
        {
            services.AddScoped<IUserIdentityResolver, AzureUserIdentityResolver>();
        }
        services.AddScoped<ISessionResolver, SessionResolver>();
        services.AddMicrosoftIdentityWebApiAuthentication(configuration, configSectionName: "AzureAdB2C");
        services.AddMicrosoftGraph();
        services.AddInMemoryTokenCaches();
        services.AddControllers();

        return services;
    }
}
