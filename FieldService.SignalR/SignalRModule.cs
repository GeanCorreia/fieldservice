using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.SignalR;

public static class SignalRModule
{
    public static IServiceCollection AddSignalRModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ISignalRGetaway, SignalRGetaway>();
        services.AddScoped<ISignalRConnectionEventProducer, BrokerSignalRConnectionEventProducer>();
        services.AddScoped<ISignalRMessageSender, SignalRMessageSender>();
        services.AddScoped<ISignalRRoomRegistry, SignalRRoomRegistry>();
        services.AddScoped<ISignalRConnectionRegistry, SignalRConnectionRegistry>();
        services.AddScoped<ISignalRReceiver, SignalRReceiver>();
        services.AddSingleton<ISignalRPresenceRegistry, RedisSignalRPresenceRegistry>();
        return services;
    }
}
