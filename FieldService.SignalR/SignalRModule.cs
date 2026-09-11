using FieldService.SignalR.Configuration;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace FieldService.SignalR;

public static class SignalRModule
{
    public static IServiceCollection AddSignalRModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Configure<SignalROptions>(configuration.GetSection(SignalROptions.SectionName));
        services.AddScoped<ISignalRConnectionEventProducer, BrokerSignalRConnectionEventProducer>();
        services.AddScoped<ISignalRMessageSender, SignalRMessageSender>();
        services.AddScoped<ISignalRRoomRegistry, SignalRRoomRegistry>();
        services.AddScoped<ISignalRConnectionRegistry, SignalRConnectionRegistry>();
        services.AddSingleton<ISignalRPresenceRegistry, RedisSignalRPresenceRegistry>();
        return services;
    }
}
