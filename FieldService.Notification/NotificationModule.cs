using FieldService.Data;
using FieldService.Notification.Configuration;
using FieldService.Notification.Data;
using FieldService.Notification.Data.Repositories;
using FieldService.Notification.Interfaces;
using FieldService.Notification.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlModule<NotificationDbContext>(configuration);
        services.Configure<NotificationCacheOptions>(configuration.GetSection(NotificationCacheOptions.SectionName));
        services.AddSingleton<INotificationCache, RedisNotificationCache>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPersistentSender, NotificationPersistentSender>();
        services.AddScoped<INotificationPersistentReceiver, NotificationPersistentReceiver>();
        services.AddHostedService<NotificationPendingOnUserConnectedConsumer>();
        return services;
    }
}
