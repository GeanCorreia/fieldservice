using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Hangfire.Tags.PostgreSql;
using FieldService.Queue.Filters;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Queue;

public static class QueueModule
{
    public static IServiceCollection AddQueueModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(HangfireQueueOptions.SectionName).Get<HangfireQueueOptions>()
                      ?? new HangfireQueueOptions();

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
                               ?? configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException(
                                   $"ConnectionStrings:{options.ConnectionStringName} is not configured and fallback ConnectionStrings:Postgres is missing.");

        services.AddSingleton(options);
        services.AddSingleton<HangfireOnCreatingFilter>();
        services.AddSingleton<HangfireExecutionFilter>();
        services.AddHangfire((serviceProvider, config) =>
        {
            var retryDelays = options.RetryDelaysInSeconds
                .Where(delay => delay > 0)
                .ToArray();
            var retryFilter = new AutomaticRetryAttribute
            {
                Attempts = Math.Max(0, options.RetryAttempts),
                OnAttemptsExceeded = AttemptsExceededAction.Fail,
                LogEvents = true
            };

            if (retryDelays.Length > 0)
                retryFilter.DelaysInSeconds = retryDelays;

            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    connectionString,
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = options.SchemaName,
                        QueuePollInterval = TimeSpan.FromSeconds(options.QueuePollIntervalSeconds),
                        InvisibilityTimeout = TimeSpan.FromMinutes(options.InvisibilityTimeoutMinutes),
                        DistributedLockTimeout = TimeSpan.FromMinutes(options.DistributedLockTimeoutMinutes),
                        PrepareSchemaIfNecessary = true
                    })
                .UseTagsWithPostgreSql()
                .UseFilter(serviceProvider.GetRequiredService<HangfireOnCreatingFilter>())
                .UseFilter(serviceProvider.GetRequiredService<HangfireExecutionFilter>())
                .UseFilter(retryFilter)
                .UseFilter(new SucceededOnlyExpirationFilter(TimeSpan.FromDays(Math.Max(1, options.SuccessfulJobRetentionDays))));
        });

        services.AddHangfireServer(serverOptions =>
        {
            var configuredQueues = options.Queues
                .Where(queue => !string.IsNullOrWhiteSpace(queue))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            serverOptions.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
            serverOptions.WorkerCount = options.WorkerCount > 0 ? options.WorkerCount : Environment.ProcessorCount * 5;
            serverOptions.Queues = configuredQueues.Length > 0 ? configuredQueues : ["default"];
            serverOptions.SchedulePollingInterval = TimeSpan.FromSeconds(options.SchedulePollingIntervalSeconds);
        });

        var recurringProducerAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && (a.FullName?.StartsWith("FieldService") == true))
            .ToArray();

        services.AddQueueProducers(recurringProducerAssemblies);
        services.AddQueueRecurringProducers(recurringProducerAssemblies);
        services.AddQueueConsumers(recurringProducerAssemblies);

        return services;
    }

    public static WebApplication UseQueueModule(this WebApplication app, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        var options = app.Services.GetRequiredService<HangfireQueueOptions>();
        if (!options.Dashboard.IsEnabledForEnvironment(environment.EnvironmentName))
            return app;

        app.UseHangfireDashboard(
            options.Dashboard.Path,
            new DashboardOptions
            {
                Authorization =
                [
                    new HangfireDashboardAuthorizationFilter(environment, options.Dashboard)
                ]
            });

        return app;
    }
}