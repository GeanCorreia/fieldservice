using FieldService.Cache.Interfaces;
using FieldService.Data;
using FieldService.SignalR.Configuration;
using FieldService.SignalR.Data;
using FieldService.SignalR.Data.Repositories;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Services;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;


namespace FieldService.SignalR;

public static class SignalRModule
{
    public static IServiceCollection AddSignalRModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSignalR()
            .AddStackExchangeRedis(options =>
            {
                options.ConnectionFactory = _ =>
                {
                    throw new InvalidOperationException(
                        "A fábrica de conexão Redis do SignalR deve ser configurada pelo contêiner de DI.");
                };
            });

        services.AddSingleton<IConfigureOptions<RedisOptions>>(sp =>
            new ConfigureOptions<RedisOptions>(options =>
            {
                options.ConnectionFactory = _ =>
                    Task.FromResult(sp.GetRequiredService<IRedisConnection>().Connection);

                options.Configuration.ChannelPrefix = RedisChannel.Literal("FieldService.SignalR");
            }));

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
