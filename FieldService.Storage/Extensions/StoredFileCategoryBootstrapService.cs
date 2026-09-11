using System.Reflection;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FieldService.Shared.Types;
using FieldService.Storage.Data;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Storage.Extensions;

internal sealed class StoredFileCategoryBootstrapService(
    StorageDbContext dbContext) : IStoredFileCategoryBootstrapService
{
    public async Task EnsureCategories(CancellationToken ct = default)
    {
        var descriptors = DiscoverCategoryDefinitions();
        ValidateDuplicateDescriptors(descriptors);

        var persistedCategories = await dbContext.StoredFileCategories
            .AsNoTracking()
            .ToListAsync(ct);

        ValidateDuplicatePersistedCategories(persistedCategories);

        var persistedByKey = persistedCategories.ToDictionary(
            x => BuildKey(x.TenantId, x.Code, x.Version.ToString()),
            x => x,
            StringComparer.Ordinal);

        var newCategories = new List<StoredFileCategory>();

        foreach (var descriptor in descriptors)
        {
            var key = BuildKey(descriptor.TenantId, descriptor.Code, descriptor.Version.ToString());
            if (!persistedByKey.TryGetValue(key, out var persisted))
            {
                newCategories.Add(new StoredFileCategory(
                    descriptor.Id,
                    descriptor.TenantId,
                    descriptor.Code,
                    descriptor.MaxSizeInBytes,
                    descriptor.AllowedContentTypes,
                    descriptor.Version,
                    descriptor.MinimumRequiredRole,
                    descriptor.AllowedPermissions));
                continue;
            }

            if (AreCompatible(persisted, descriptor))
                continue;

            throw new InvalidOperationException(
                $"Stored file category mismatch for '{descriptor.Type.FullName}' ({descriptor.TenantId}/{descriptor.Code} v{descriptor.Version}). Stored category differs from current definition.");
        }

        if (newCategories.Count > 0)
        {
            await dbContext.StoredFileCategories.AddRangeAsync(newCategories, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private static IReadOnlyCollection<CategoryDescriptor> DiscoverCategoryDefinitions()
    {
        var definitionTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(IsFieldServiceAssembly)
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                !type.IsAbstract &&
                !type.ContainsGenericParameters &&
                typeof(IStoredFileCategoryDefinition).IsAssignableFrom(type))
            .ToArray();

        var descriptors = new List<CategoryDescriptor>(definitionTypes.Length);
        foreach (var definitionType in definitionTypes)
        {
            descriptors.Add(new CategoryDescriptor(
                definitionType,
                GetRequiredStaticValue<Guid>(definitionType, nameof(IStoredFileCategoryDefinition.FileTenantId)),
                GetRequiredStaticValue<string>(definitionType, nameof(IStoredFileCategoryDefinition.FileCode)),
                GetOptionalStaticValue<long?>(definitionType, nameof(IStoredFileCategoryDefinition.FileMaxSizeInBytes)),
                NormalizeContentTypes(GetRequiredStaticValue<IEnumerable<MediaTypeHeaderValue>>(definitionType, nameof(IStoredFileCategoryDefinition.FileAllowedContentTypes))),
                GetRequiredStaticValue<SchemaVersion>(definitionType, nameof(IStoredFileCategoryDefinition.FileVersion)),
                GetOptionalStaticValue<Role?>(definitionType, nameof(IStoredFileCategoryDefinition.FileMinimumRequiredRole)),
                NormalizePermissions(GetRequiredStaticValue<IReadOnlyList<Permission>>(definitionType, nameof(IStoredFileCategoryDefinition.FileAllowedPermissions)))));
        }

        return descriptors;
    }

    private static bool IsFieldServiceAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
            return false;

        var assemblyName = assembly.GetName().Name;
        return assemblyName is not null && assemblyName.StartsWith("FieldService.", StringComparison.Ordinal);
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

    private static void ValidateDuplicateDescriptors(IReadOnlyCollection<CategoryDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(x => BuildKey(x.TenantId, x.Code, x.Version.ToString()), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        if (duplicates.Length == 0)
            return;

        var duplicateDetails = string.Join(
            "; ",
            duplicates.Select(group =>
                $"{group.Key} => {string.Join(", ", group.Select(x => x.Type.FullName))}"));

        throw new InvalidOperationException(
            $"Duplicate stored file category identity detected. Each TenantId+Code+Version must be unique. {duplicateDetails}");
    }

    private static void ValidateDuplicatePersistedCategories(IReadOnlyCollection<StoredFileCategory> categories)
    {
        var duplicates = categories
            .GroupBy(x => BuildKey(x.TenantId, x.Code, x.Version.ToString()), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        if (duplicates.Length == 0)
            return;

        var duplicateDetails = string.Join(
            "; ",
            duplicates.Select(group =>
                $"{group.Key} => {string.Join(", ", group.Select(x => x.Id))}"));

        throw new InvalidOperationException(
            $"Duplicate stored file category rows detected in database. Each TenantId+Code+Version must be unique. {duplicateDetails}");
    }

    private static string BuildKey(Guid tenantId, string code, string version)
        => $"{tenantId}::{code}::{version}";

    private static bool AreCompatible(StoredFileCategory persisted, CategoryDescriptor descriptor)
    {
        if (persisted.TenantId != descriptor.TenantId)
            return false;

        if (!string.Equals(persisted.Code, descriptor.Code, StringComparison.Ordinal))
            return false;

        if (!Equals(persisted.MaxSizeInBytes, descriptor.MaxSizeInBytes))
            return false;

        if (persisted.Version != descriptor.Version)
            return false;

        if (!Equals(persisted.MinimumRequiredRole, descriptor.MinimumRequiredRole))
            return false;

        return Enumerable.SequenceEqual(
                   NormalizeContentTypeStrings(persisted.AllowedContentTypes),
                   NormalizeContentTypeStrings(descriptor.AllowedContentTypes),
                   StringComparer.OrdinalIgnoreCase)
               && Enumerable.SequenceEqual(
                   NormalizePermissions(persisted.AllowedPermissions ?? Array.Empty<Permission>()),
                   NormalizePermissions(descriptor.AllowedPermissions),
                   PermissionComparer.Instance);
    }

    private static IReadOnlyList<MediaTypeHeaderValue> NormalizeContentTypes(IEnumerable<MediaTypeHeaderValue> values)
        => values.Where(value => value is not null)
            .OrderBy(value => value!.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

    private static IReadOnlyList<string> NormalizeContentTypeStrings(IEnumerable<MediaTypeHeaderValue> values)
        => values.Select(value => value.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<Permission> NormalizePermissions(IEnumerable<Permission> values)
        => values.Where(value => value is not null)
            .OrderBy(value => value.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static T GetRequiredStaticValue<T>(Type type, string memberName)
    {
        var value = GetStaticValue<T?>(type, memberName);
        if (value is null)
            throw new InvalidOperationException($"Stored file category definition '{type.FullName}' is missing required static member '{memberName}'.");

        if (value is string text && string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"Stored file category definition '{type.FullName}' has empty static member '{memberName}'.");

        return value;
    }

    private static T? GetOptionalStaticValue<T>(Type type, string memberName)
    {
        var memberType = typeof(T);
        var value = GetStaticValue<object?>(type, memberName);
        if (value is null)
            return default;

        var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (targetType.IsAssignableFrom(value.GetType()))
            return (T)value;

        if (targetType.IsEnum)
        {
            if (value is string text)
                return (T)Enum.Parse(targetType, text, ignoreCase: true);

            return (T)Enum.ToObject(targetType, value);
        }

        return (T)Convert.ChangeType(value, targetType);
    }

    private static T? GetStaticValue<T>(Type type, string memberName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        var field = type.GetField(memberName, flags);
        if (field is not null)
            return (T?)field.GetValue(null);

        var property = type.GetProperty(memberName, flags);
        if (property is not null)
            return (T?)property.GetValue(null);

        throw new InvalidOperationException($"Stored file category definition '{type.FullName}' does not expose static member '{memberName}'.");
    }

    private sealed record CategoryDescriptor(
        Type Type,
        Guid TenantId,
        string Code,
        long? MaxSizeInBytes,
        IReadOnlyList<MediaTypeHeaderValue> AllowedContentTypes,
        SchemaVersion Version,
        Role? MinimumRequiredRole,
        IReadOnlyList<Permission> AllowedPermissions)
    {
        public Guid Id => Guid.NewGuid();
    }

    private sealed class PermissionComparer : IEqualityComparer<Permission>
    {
        public static PermissionComparer Instance { get; } = new();

        public bool Equals(Permission? x, Permission? y)
            => string.Equals(x?.ToString(), y?.ToString(), StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(Permission obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToString());
    }
}




