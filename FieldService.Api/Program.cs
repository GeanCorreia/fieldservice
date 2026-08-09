using FieldService.Broker;
using FieldService.Authentication;
using FieldService.Authorization;
using FieldService.Cache;
using FieldService.Data;
using FieldService.Http;
using FieldService.Observability;
using FieldService.Queue;
using FieldService.Shared;
using FiledService.Audit;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .Sources.Clear();

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

if (builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Test") ||
    builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
}
else
{
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddObservabilityModule(builder.Configuration, builder.Environment);
builder.Services.AddSharedModule();
builder.Services.AddAuthenticationModule(builder.Configuration, builder.Environment);
builder.Services.AddAuthorizationModule(builder.Configuration);
builder.Services.AddDataModule(builder.Configuration, builder.Environment);
builder.Services.AddBrokerModule(builder.Configuration);
builder.Services.AddCacheModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddQueueModule(builder.Configuration);
builder.Services.AddHttpPipeline();
// builder.Services.AddNotificationModule(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpPipeline(builder.Environment);
app.UseQueueModule(builder.Environment);
app.MapGet("/", () => "FieldService API running.");

app.Run();