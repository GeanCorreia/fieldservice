using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Shared.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Services;

public sealed class AzureServiceBusDispatcher : IBrokerDispatcher
{
    private static readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _batchTasks = new(StringComparer.OrdinalIgnoreCase);

    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AzureServiceBusDispatcher> _logger;

    public AzureServiceBusDispatcher(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        ILogger<AzureServiceBusDispatcher> logger)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RegisterConsumersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        await DiscoverAndRegisterAsync(sp, typeof(IBrokerConsumer<>), nameof(RegisterConsumerAsync), ct);
        await DiscoverAndRegisterAsync(sp, typeof(IBrokerBatchConsumer<>), nameof(RegisterBatchConsumerAsync), ct);
    }

    private async Task DiscoverAndRegisterAsync(
        IServiceProvider sp,
        Type openGenericInterface,
        string privateMethodName,
        CancellationToken ct)
    {
        var consumerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => new
            {
                Type = t,
                Interface = t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
            })
            .Where(x => x.Interface != null)
            .ToList();

        var method = typeof(AzureServiceBusDispatcher)
            .GetMethod(privateMethodName, BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var item in consumerTypes)
        {
            var payloadType = item.Interface!.GetGenericArguments()[0];
            var consumerInstance = sp.GetService(item.Type);

            if (consumerInstance is null)
            {
                _logger.LogWarning("[Dispatcher] Consumer '{ConsumerType}' found but not registered in DI container.", item.Type.Name);
                continue;
            }

            var genericMethod = method.MakeGenericMethod(item.Type, payloadType);
            await (Task)genericMethod.Invoke(this, [consumerInstance, ct])!;
        }
    }

    private async Task RegisterConsumerAsync<TConsumer, TPayload>(TConsumer consumer, CancellationToken ct = default)
        where TConsumer : IBrokerConsumer<TPayload>
        where TPayload : class, IMessagePayload
    {
        BrokerSubscribeContext sub = ResolveSubscriptionContext(typeof(TConsumer));
        ValidateSubscriptionUniqueness(sub.EntityName, sub.SubscriptionName);

        var key = $"ind:{sub.EntityName}:{sub.SubscriptionName}";
        if (_processors.ContainsKey(key)) return;

        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = sub.PrefetchCount,
            MaxConcurrentCalls = sub.ConcurrentMessageLimit ?? 1,
            MaxAutoLockRenewalDuration = sub.AutoRenewTimeout ?? TimeSpan.FromMinutes(5)
        };

        var processor = _serviceBusClient.CreateProcessor(sub.EntityName, sub.SubscriptionName, options);

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedConsumer = scope.ServiceProvider.GetRequiredService<TConsumer>();

                var message = DeserializeMessage<TPayload>(args.Message.Body);
                if (message is not null)
                {
                    await scopedConsumer.ConsumeAsync(message, args.CancellationToken);
                }

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dispatcher] Error processing individual message in '{Consumer}'.", typeof(TConsumer).Name);
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "[Dispatcher] Exception on ServiceBusProcessor for path '{EntityPath}'.", args.EntityPath);
            return Task.CompletedTask;
        };

        _processors[key] = processor;
        await processor.StartProcessingAsync(ct);
        
        _logger.LogInformation("[Dispatcher] Registered Individual Consumer '{Consumer}' on '{Entity}/{Sub}'.", 
            typeof(TConsumer).Name, sub.EntityName, sub.SubscriptionName);
    }

    private async Task RegisterBatchConsumerAsync<TConsumer, TPayload>(TConsumer consumer, CancellationToken ct = default)
        where TConsumer : IBrokerBatchConsumer<TPayload>
        where TPayload : class, IMessagePayload
    {
        ArgumentNullException.ThrowIfNull(consumer);

        BrokerSubscribeContext sub = ResolveSubscriptionContext(typeof(TConsumer));
        ValidateSubscriptionUniqueness(sub.EntityName, sub.SubscriptionName);

        var key = $"batch:{sub.EntityName}:{sub.SubscriptionName}";
        if (_batchTasks.ContainsKey(key)) return;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _batchTasks[key] = cts;

        _ = Task.Run(() => StartBatchLoopAsync<TConsumer, TPayload>(sub, cts.Token), cts.Token);

        _logger.LogInformation("[Dispatcher] Registered Batch Consumer '{Consumer}' on '{Entity}/{Sub}'.", 
            typeof(TConsumer).Name, sub.EntityName, sub.SubscriptionName);

        await Task.CompletedTask;
    }

    private async Task StartBatchLoopAsync<TConsumer, TPayload>(BrokerSubscribeContext sub, CancellationToken ct)
        where TConsumer : IBrokerBatchConsumer<TPayload>
        where TPayload : class, IMessagePayload
    {
        var receiver = _serviceBusClient.CreateReceiver(
            sub.EntityName,
            sub.SubscriptionName,
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });

        int batchSize = sub.BatchSize > 0 ? sub.BatchSize : 10;
        TimeSpan maxWaitTime = sub.BatchTimeout ?? TimeSpan.FromSeconds(5);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var receivedMessages = await receiver.ReceiveMessagesAsync(batchSize, maxWaitTime, ct);
                if (receivedMessages is null || receivedMessages.Count == 0) continue;

                var batchToProcess = new List<IMessage<TPayload>>();
                foreach (var sbMessage in receivedMessages)
                {
                    var message = DeserializeMessage<TPayload>(sbMessage.Body);
                    if (message is not null)
                    {
                        batchToProcess.Add(message);
                    }
                }

                if (batchToProcess.Count > 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedConsumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
                    await scopedConsumer.ConsumeAsync(batchToProcess, ct);
                }

                foreach (var sbMessage in receivedMessages)
                {
                    await receiver.CompleteMessageAsync(sbMessage, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await Task.Delay(2000, ct);
            }
        }

        await receiver.DisposeAsync();
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        foreach (var processor in _processors.Values)
        {
            if (!processor.IsProcessing)
            {
                await processor.StartProcessingAsync(ct);
            }
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var processor in _processors.Values)
        {
            if (processor.IsProcessing)
            {
                await processor.StopProcessingAsync(ct);
            }
        }

        foreach (var cts in _batchTasks.Values)
        {
            cts.Cancel();
        }
    }

    public bool IsListening(string entityName, string subscriptionName)
    {
        var indKey = $"ind:{entityName}:{subscriptionName}";
        var batchKey = $"batch:{entityName}:{subscriptionName}";

        if (_processors.TryGetValue(indKey, out var processor))
        {
            return processor.IsProcessing;
        }

        return _batchTasks.ContainsKey(batchKey);
    }

    private static void ValidateSubscriptionUniqueness(string entityName, string subscriptionName)
    {
        var indKey = $"ind:{entityName}:{subscriptionName}";
        var batchKey = $"batch:{entityName}:{subscriptionName}";

        if (_processors.ContainsKey(indKey) || _batchTasks.ContainsKey(batchKey))
        {
            throw new InvalidOperationException(
                $"Subscription '{subscriptionName}' on Entity '{entityName}' is already registered. " +
                "A subscription can only support ONE listener type (Individual OR Batch).");
        }
    }

    private static BrokerSubscribeContext ResolveSubscriptionContext(Type consumerType)
    {
        var contextProperty = consumerType.GetProperty(
            nameof(IBrokerConsumer.BrokerSubscriptionContext),
            BindingFlags.Public | BindingFlags.Static);

        if (contextProperty?.GetValue(null) is BrokerSubscribeContext context)
            return context;

        throw new InvalidOperationException(
            $"Consumer '{consumerType.FullName}' must expose a public static '{nameof(IBrokerConsumer.BrokerSubscriptionContext)}' property.");
    }

    private static IMessage<TPayload>? DeserializeMessage<TPayload>(BinaryData body)
        where TPayload : class, IMessagePayload
    {
        ArgumentNullException.ThrowIfNull(body);
        return JsonSerializer.Deserialize<TransportMessage<TPayload>>(body.ToStream());
    }

    private sealed record TransportMessage<TPayload> : IMessage<TPayload>
        where TPayload : class, IMessagePayload
    {
        public required TPayload Payload { get; init; }
        public required MessageContext Context { get; init; }
        IMessagePayload IMessage.Payload => Payload;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        foreach (var processor in _processors.Values)
        {
            await processor.DisposeAsync();
        }

        foreach (var cts in _batchTasks.Values)
        {
            cts.Dispose();
        }

        _processors.Clear();
        _batchTasks.Clear();
    }
}