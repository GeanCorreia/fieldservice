namespace FieldService.Broker.Configuration;

public sealed class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";
    public string ConnectionString { get; init; } = string.Empty;
}
