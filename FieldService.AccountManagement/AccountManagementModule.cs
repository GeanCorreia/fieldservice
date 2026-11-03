using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Data.Repositories;
using FieldService.AccountManagement.Interfaces;
using FieldService.AccountManagement.Services;
using FieldService.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.AccountManagement;

public static class AccountManagementModule
{
    public static IServiceCollection AddAccountManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlModule<AccountManagementDbContext>(configuration);
        services.AddScoped<IAccountManagementRepository, AccountManagementRepository>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();

        return services;
    }
}
