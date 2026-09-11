using FieldService.Data;
using FieldService.Notification.Broker.Consumers;
using FieldService.Notification.Configuration;
using FieldService.Notification.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSqlModule<NotificationDbContext>(configuration);
        services.Configure<NotificationCacheOptions>(configuration.GetSection(NotificationCacheOptions.SectionName));

        return services;
    }
}
