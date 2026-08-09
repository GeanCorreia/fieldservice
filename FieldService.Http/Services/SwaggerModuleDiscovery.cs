using System.Reflection;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FieldService.Http.Services;

internal static class SwaggerModuleDiscovery
{
    private const string DefaultModuleName = "Api";
    private const string DefaultSwaggerVersion = "v1";

    public static void AddSwagger(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SessionIdHeaderOperationFilter>();

            var modules = DiscoverModulesWithVersions();
            foreach (var module in modules)
            {
                options.SwaggerDoc(module.ModuleName, new OpenApiInfo
                {
                    Title = $"FieldService {module.ModuleName}",
                    Version = module.SwaggerVersion
                });
            }

            options.DocInclusionPredicate((docName, apiDescription) =>
            {
                if (apiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
                    return false;

                var moduleName = GetModuleName(actionDescriptor.ControllerTypeInfo.Namespace);
                return string.Equals(moduleName, docName, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    public static IReadOnlyCollection<(string ModuleName, string SwaggerVersion)> DiscoverModulesWithVersions()
    {
        var controllerModules = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(GetTypesSafely)
            .Where(type =>
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type is { IsAbstract: false, IsClass: true })
            .Select(type => GetModuleName(type.Namespace))
            .Where(module => !string.IsNullOrWhiteSpace(module))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var moduleVersions = DiscoverModuleVersions();

        var discovered = controllerModules
            .Select(moduleName =>
            {
                var version = moduleVersions.TryGetValue(moduleName, out var configuredVersion)
                    ? configuredVersion
                    : DefaultSwaggerVersion;

                return (ModuleName: moduleName, SwaggerVersion: version);
            })
            .OrderBy(module => module.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (discovered.Length > 0)
            return discovered;

        return [(DefaultModuleName, DefaultSwaggerVersion)];
    }

    public static string GetModuleName(string? typeNamespace)
    {
        if (string.IsNullOrWhiteSpace(typeNamespace))
            return DefaultModuleName;

        var parts = typeNamespace.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 &&
            (string.Equals(parts[0], "FieldService", StringComparison.Ordinal) ||
             string.Equals(parts[0], "FiledService", StringComparison.Ordinal))) // Previne o seu typo digitado!
        {
            return parts[1];
        }

        return DefaultModuleName;
    }

    private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null)!;
        }
    }

    private static Dictionary<string, string> DiscoverModuleVersions()
    {
        var versionProviders = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(GetTypesSafely)
            .Where(type =>
                typeof(IApiVersion).IsAssignableFrom(type) &&
                type is { IsAbstract: false, IsClass: true } &&
                type.GetConstructor(Type.EmptyTypes) != null)
            .ToArray();

        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var providerType in versionProviders)
        {
            var moduleName = GetModuleName(providerType.Namespace);
            if (string.IsNullOrWhiteSpace(moduleName))
                continue;

            var instance = (IApiVersion?)Activator.CreateInstance(providerType);
            if (instance == null)
                continue;

            versions[moduleName] = ToSwaggerVersion(instance.SchemaVersion);
        }

        return versions;
    }

    private static string ToSwaggerVersion(SchemaVersion schemaVersion)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        return $"v{schemaVersion.Major}.{schemaVersion.Minor}.{schemaVersion.Patch}";
    }
}
