using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FieldService.Observability;

public static class ObservabilityModule
{
    public static IServiceCollection AddObservabilityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var serviceName = configuration["Observability:ServiceName"]
            ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? environment.ApplicationName;

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
                options.SetResourceBuilder(
                    ResourceBuilder.CreateDefault().AddService(serviceName: serviceName));
            });
        });

        var telemetryBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName: serviceName);
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.EnrichWithHttpRequest = static (activity, httpRequest) =>
                    {
                        var clientAddress = httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString();
                        if (!string.IsNullOrWhiteSpace(clientAddress))
                            activity.SetTag("client.address", clientAddress);
                    };
                });
                tracing.AddHttpClientInstrumentation();
                tracing.AddSource(
                    "Grpc.Net.Client",
                    "Grpc.AspNetCore.Server",
                    "Microsoft.AspNetCore.SignalR.Server",
                    "Microsoft.AspNetCore.SignalR.Client",
                    "FieldService.Queue.Hangfire",
                    "MassTransit");
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddMeter(
                    "Grpc.AspNetCore.Server",
                    "Microsoft.AspNetCore.Http.Connections",
                    "Microsoft.AspNetCore.SignalR",
                    "Grpc.Net.Client",
                    "FieldService.Queue.Hangfire",
                    "MassTransit");
            });

        if (environment.IsDevelopment())
        {
            var otlpEndpoint = configuration["Observability:Aspire:OtlpEndpoint"] ?? "http://localhost:18889";
            var endpointUri = new Uri(otlpEndpoint, UriKind.Absolute);

            telemetryBuilder
                .WithTracing(tracing =>
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = endpointUri;
                        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                    });
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = endpointUri;
                        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                    });
                });

            services.Configure<OpenTelemetryLoggerOptions>(options =>
            {
                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = endpointUri;
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        }
        else
        {
            var connectionString = configuration.GetConnectionString("AzureMonitor")
                ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                ?? throw new InvalidOperationException(
                    "Azure Monitor connection string is missing. Configure 'ConnectionStrings:AzureMonitor' or 'APPLICATIONINSIGHTS_CONNECTION_STRING'.");

            telemetryBuilder.UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });
        }

        return services;
    }
}
