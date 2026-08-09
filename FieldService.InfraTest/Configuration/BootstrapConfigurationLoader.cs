using Microsoft.Extensions.Configuration;

namespace FieldService.InfraTest.Configuration;

internal static class BootstrapConfigurationLoader
{
    public static IConfigurationRoot LoadDevelopmentConfiguration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configurationPath = Path.Combine(repositoryRoot, "FieldService.Api", "appsettings.Development.json");

        return new ConfigurationBuilder()
            .AddJsonFile(configurationPath, optional: false, reloadOnChange: false)
            .Build();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "FieldService.sln");
            if (File.Exists(solutionPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }
}
