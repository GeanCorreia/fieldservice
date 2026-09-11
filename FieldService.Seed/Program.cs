namespace FieldService.ConsoleTests;

internal static class Program
{
    private static async Task Main()
    {
        await DevelopmentBrokerConfiguratorRunner.Run();
    }
}
