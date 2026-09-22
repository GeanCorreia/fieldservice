using FieldService.Cache.Interfaces;
using FieldService.Data;
using FieldService.SignalR.Configuration;
using FieldService.SignalR.Data;
using FieldService.SignalR.Data.Repositories;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;


namespace FieldService.SignalR;

public static class SignalRModule
{
    public static IServiceCollection AddSignalRModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSignalR()
            .AddStackExchangeRedis(options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("FieldService.SignalR");
            });

        services.AddSqlModule<SignalRDbContext>(configuration);
        
        services.AddSingleton<IConnectionMultiplexer>(sp => 
        {
            var redisContext = sp.GetRequiredService<IRedisConnection>();
            return redisContext.Connection;
        });
        
        services.Configure<SignalROptions>(configuration.GetSection(SignalROptions.SectionName));
        services.AddScoped<ISignalRConnectionEventProducer, BrokerSignalRConnectionEventProducer>();
        services.AddScoped<ISignalRMessageSender, SignalRMessageSender>();
        services.AddScoped<ISignalRConnectionManager, SignalRConnectionManager>();
        services.AddScoped<IDomainRoomRepository, DomainRoomRepository>();
        services.AddScoped<IDomainRoomManager, DomainRoomManager>();
        services.AddScoped<IDomainRoomService, DomainRoomService>();
        services.AddScoped<IPresenceRegistry, PresenceRegistry>();
        
        return services;
    }

    public static IEndpointRouteBuilder UseSignalRModule(this IEndpointRouteBuilder app)
    {
        app.MapHub<SignalRHub>("/hubs/signalr");
        return app;
    }
}
