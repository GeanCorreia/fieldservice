using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
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

        var telemetryBuilder = services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSource(
                    "Grpc.Net.Client",
                    "Grpc.AspNetCore.Server",
                    "Microsoft.AspNetCore.SignalR.Server",
                    "Microsoft.AspNetCore.SignalR.Client",
                    "FieldService.Queue.Hangfire");
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
                    "FieldService.Queue.Hangfire");
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
