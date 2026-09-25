using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FieldService.Shared.Dtos;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using Microsoft.Extensions.DependencyModel;

namespace FiledService.Audit.Services;

public sealed class AuditDtoSchemaBootstrapService(
    IAuditDtoSchemaRepository auditDtoSchemaRepository) : IAuditDtoSchemaBootstrapService
{
    public async Task EnsureSchemas(CancellationToken ct = default)
    {
        var descriptors = DiscoverDtoSchemas();
        ValidateDuplicateDescriptors(descriptors);

        var persistedSchemas = await auditDtoSchemaRepository.GetAll(ct);
        var persistedByKey = persistedSchemas.ToDictionary(
            x => BuildKey(x.ResourceName, x.Version),
            x => x,
            StringComparer.Ordinal);

        var schemasToPersist = new List<AuditDtoSchema>();
        foreach (var descriptor in descriptors)
        {
            var key = BuildKey(descriptor.ResourceName, descriptor.Version.ToString());
            if (!persistedByKey.TryGetValue(key, out var persisted))
            {
                schemasToPersist.Add(AuditDtoSchema.Create(
                    descriptor.ResourceName,
                    descriptor.Version,
                    descriptor.Properties,
                    DateTimeOffset.UtcNow));
                continue;
            }

            if (Canonicalize(persisted.Properties) == Canonicalize(descriptor.Properties))
                continue;

            schemasToPersist.Add(AuditDtoSchema.Create(
                descriptor.ResourceName,
                descriptor.Version,
                descriptor.Properties,
                DateTimeOffset.UtcNow));
        }

        await auditDtoSchemaRepository.Save(schemasToPersist, ct);
    }

    private static IReadOnlyCollection<DtoSchemaDescriptor> DiscoverDtoSchemas()
    {
        var dtoTypes = GetFieldServiceAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                !type.IsAbstract &&
                !type.ContainsGenericParameters &&
                typeof(AbstractDto).IsAssignableFrom(type))
            .ToArray();

        var descriptors = new List<DtoSchemaDescriptor>(dtoTypes.Length);
        foreach (var dtoType in dtoTypes)
        {
            var dto = (AbstractDto)RuntimeHelpers.GetUninitializedObject(dtoType);
            descriptors.Add(new DtoSchemaDescriptor(
                dtoType,
                dto.ResourceName,
                dto.Version,
                dto.Properties));
        }

        return descriptors;
    }

    private static IReadOnlyCollection<Assembly> GetFieldServiceAssemblies()
    {
        var loadedAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(IsFieldServiceAssembly)
            .ToDictionary(assembly => assembly.GetName().Name!, StringComparer.Ordinal);

        var runtimeLibraries = DependencyContext.Default?.RuntimeLibraries
            .Where(library => library.Name.StartsWith("FieldService.", StringComparison.Ordinal))
            .ToArray();

        if (runtimeLibraries is null || runtimeLibraries.Length == 0)
            return loadedAssemblies.Values.ToArray();

        foreach (var runtimeLibrary in runtimeLibraries)
        {
            if (loadedAssemblies.ContainsKey(runtimeLibrary.Name))
                continue;

            try
            {
                var assembly = Assembly.Load(new AssemblyName(runtimeLibrary.Name));
                if (IsFieldServiceAssembly(assembly))
                    loadedAssemblies[assembly.GetName().Name!] = assembly;
            }
            catch
            {
                // Best-effort assembly loading for schema discovery.
            }
        }

        return loadedAssemblies.Values.ToArray();
    }

    private static bool IsFieldServiceAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
            return false;

        var assemblyName = assembly.GetName().Name;
        return assemblyName is not null &&
               assemblyName.StartsWith("FieldService.", StringComparison.Ordinal);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static void ValidateDuplicateDescriptors(IReadOnlyCollection<DtoSchemaDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(x => BuildKey(x.ResourceName, x.Version.ToString()), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        if (duplicates.Length == 0)
            return;

        var duplicateDetails = string.Join(
            "; ",
            duplicates.Select(group =>
                $"{group.Key} => {string.Join(", ", group.Select(x => x.Type.FullName))}"));

        throw new InvalidOperationException(
            $"Duplicate DTO schema identity detected. Each ResourceName+Version must be unique. {duplicateDetails}");
    }

    private static string BuildKey(string resourceName, string version)
        => $"{resourceName}::{version}";

    private static string Canonicalize(JsonElement element)
    {
        var builder = new StringBuilder();
        AppendCanonical(builder, element);
        return builder.ToString();
    }

    private static void AppendCanonical(StringBuilder builder, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var first = true;
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    if (!first)
                        builder.Append(',');

                    first = false;
                    builder.Append(property.Name);
                    builder.Append(':');
                    AppendCanonical(builder, property.Value);
                }

                builder.Append('}');
                break;

            case JsonValueKind.Array:
                builder.Append('[');
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (index > 0)
                        builder.Append(',');

                    AppendCanonical(builder, item);
                    index++;
                }

                builder.Append(']');
                break;

            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    private sealed record DtoSchemaDescriptor(
        Type Type,
        string ResourceName,
        FieldService.Shared.Types.SchemaVersion Version,
        JsonElement Properties);
}
