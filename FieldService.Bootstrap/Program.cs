using FieldService.Broker;
using FieldService.Cache;
using FieldService.Data;
using FieldService.Notification;
using FieldService.Bootstrap.Services;
using FieldService.SignalR;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// Add services to the container.
builder.Services.AddGrpc();
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString);
}
builder.Services.AddMongoModule(builder.Configuration);
builder.Services.AddBrokerModule(builder.Configuration);
builder.Services.AddCacheModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);
builder.Services.AddSignalRModule();
builder.Services.AddScoped<ISignalRAckProcessor, NotificationSignalRAckProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapHub<SignalRHub>("/hubs/notifications");
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. " +
        "To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();