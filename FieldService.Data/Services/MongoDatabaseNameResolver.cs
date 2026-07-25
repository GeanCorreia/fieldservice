using Microsoft.Extensions.Options;
using System.Text;

namespace FieldService.Data.Services;

internal sealed class MongoDatabaseNameResolver(IOptions<MongoDbOptions> options) : IMongoDatabaseNameResolver
{
    private readonly string _defaultDatabase = options.Value.Database;
    private readonly Dictionary<string, string> _moduleDatabases =
        new(options.Value.ModuleDatabases ?? [], StringComparer.OrdinalIgnoreCase);

    public string Resolve(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        if (_moduleDatabases.TryGetValue(moduleName, out var configuredDatabase))
            return configuredDatabase;

        return $"{_defaultDatabase}_{NormalizeModuleName(moduleName)}";
    }

    private static string NormalizeModuleName(string moduleName)
    {
        var normalized = new StringBuilder(moduleName.Length);
        foreach (var ch in moduleName.Trim())
            normalized.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');

        return normalized.ToString();
    }
}
