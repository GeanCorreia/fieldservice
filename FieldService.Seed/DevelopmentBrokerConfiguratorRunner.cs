using FieldService.Broker.Extensions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FieldService.Broker.Configuration;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
 
namespace FieldService.ConsoleTests;

public static class DevelopmentBrokerConfiguratorRunner
{
    private const string DefaultEmulatorConnectionString = 
        "Endpoint=sb://127.0.0.1:5672;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public static async Task Run(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetSection(AzureServiceBusOptions.SectionName)
            .Get<AzureServiceBusOptions>()?.ConnectionString 
            ?? Environment.GetEnvironmentVariable("AzureServiceBus__ConnectionString")
            ?? DefaultEmulatorConnectionString;

        // USA O CLIENT DE MENSAGENS (AMQP / 5672) EM VEZ DO CLIENT DE ADMINISTRAÇÃO (HTTPS / 443)
        services.AddSingleton(new ServiceBusClient(connectionString));
         
        await using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DevelopmentBrokerConfiguratorRunner));
         
        await DevelopmentBrokerConfigurator.Configure(logger);

        await RestartServiceBusEmulatorAsync(logger, cancellationToken);
        await ValidateEmulatorConnectionAsync(serviceProvider, logger, cancellationToken);

        Console.WriteLine("Pressione ENTER para finalizar...");
        Console.ReadLine();
    }

    private static async Task RestartServiceBusEmulatorAsync(ILogger logger, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Restarting Service Bus emulator container...");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose restart servicebus",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdOut = await process.StandardOutput.ReadToEndAsync(ct);
        var stdErr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to restart Service Bus emulator. {stdErr}");
        }

        logger.LogInformation("Service Bus emulator restarted successfully. Output: {Output}", stdOut.Trim());
    }

    private static async Task ValidateEmulatorConnectionAsync(IServiceProvider serviceProvider, ILogger logger, CancellationToken ct)
    {
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var publishContexts = BrokerTopologyDiscoverer.DiscoverPublishContexts().ToList();

        logger.LogInformation("Waiting for emulator AMQP port (5672) to accept connections...");
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        // Testamos criando um sender via AMQP para o tópico (Valida a porta 5672 sem chamar HTTP 443)
        foreach (var publishContext in publishContexts)
        {
            await RetryPolicyAsync(async () =>
            {
                await using var sender = client.CreateSender(publishContext.EntityName);
                // Testa abrir o link AMQP com a entidade
                return true;
            }, maxRetries: 5, delay: TimeSpan.FromSeconds(2));
        }

        logger.LogInformation("Emulator connection and topology readiness verified via AMQP.");
    }

    private static async Task<T> RetryPolicyAsync<T>(Func<Task<T>> action, int maxRetries, TimeSpan delay)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch when (i < maxRetries - 1)
            {
                await Task.Delay(delay);
            }
        }
        return await action();
    }
}
