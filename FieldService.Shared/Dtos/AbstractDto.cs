using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public abstract record AbstractDto
{
    [JsonIgnore]
    public abstract bool IsActive { get; }
    [JsonIgnore]
    public abstract SchemaVersion Version { get; }
    [JsonIgnore]
    public abstract string ResourceName { get; }
    [JsonIgnore]
    public abstract Guid? ResourceId { get; }

    [JsonIgnore]
    public JsonElement Properties => BuildPropertiesJson(GetType());

    public static Guid? ResolveResourceId(AbstractDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto.ResourceId;
    }

    private static JsonElement BuildPropertiesJson(Type rootType)
    {
        var tree = new Dictionary<string, object?>(StringComparer.Ordinal);
        CollectPropertyTree(rootType, tree, depth: 0);
        using var document = JsonSerializer.SerializeToDocument(tree);
        return document.RootElement.Clone();
    }

    private static void CollectPropertyTree(
        Type type,
        IDictionary<string, object?> tree,
        int depth)
    {
        if (depth > 6)
            return;

        foreach (var property in type
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;
            if (property.Name is nameof(Version) or nameof(ResourceName) or nameof(ResourceId) or nameof(Properties))
                continue;

            var propertyType = property.PropertyType;
            if (TryGetCollectionElementType(propertyType, out var elementType))
            {
                if (IsComplexType(elementType))
                {
                    var itemNode = new Dictionary<string, object?>(StringComparer.Ordinal);
                    CollectPropertyTree(elementType, itemNode, depth + 1);
                    tree[$"{property.Name} : {FormatTypeName(propertyType)}"] = new object?[] { itemNode };
                }
                else
                {
                    tree[$"{property.Name} : {FormatTypeName(propertyType)}"] = Array.Empty<object>();
                }

                continue;
            }

            if (IsComplexType(propertyType))
            {
                var childNode = new Dictionary<string, object?>(StringComparer.Ordinal);
                CollectPropertyTree(propertyType, childNode, depth + 1);
                tree[$"{property.Name} : {FormatTypeName(propertyType)}"] = childNode;
            }
            else
            {
                tree[$"{property.Name} : {FormatTypeName(propertyType)}"] = new Dictionary<string, object?>(StringComparer.Ordinal);
            }
        }
    }

    private static string FormatTypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return $"{FormatTypeName(underlyingType)}?";

        if (type.IsArray)
            return $"{FormatTypeName(type.GetElementType()!)}[]";

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericName = genericTypeDefinition.Name;
            var backtickIndex = genericName.IndexOf('`');
            if (backtickIndex >= 0)
                genericName = genericName[..backtickIndex];

            var arguments = type.GetGenericArguments()
                .Select(FormatTypeName);

            return $"{genericName}<{string.Join(", ", arguments)}>";
        }

        return type.Name switch
        {
            nameof(String) => "string",
            nameof(Boolean) => "bool",
            nameof(Int16) => "short",
            nameof(Int32) => "int",
            nameof(Int64) => "long",
            nameof(UInt16) => "ushort",
            nameof(UInt32) => "uint",
            nameof(UInt64) => "ulong",
            nameof(Byte) => "byte",
            nameof(SByte) => "sbyte",
            nameof(Single) => "float",
            nameof(Double) => "double",
            nameof(Decimal) => "decimal",
            nameof(Char) => "char",
            nameof(Object) => "object",
            _ => type.Name
        };
    }

    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        elementType = default!;

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (!type.IsGenericType)
            return false;

        if (type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableInterface is null)
                return false;

            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = type.GetGenericArguments()[0];
        return true;
    }

    private static bool IsComplexType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            type = underlyingType;

        return !(type.IsPrimitive ||
                 type.IsEnum ||
                 type == typeof(string) ||
                 type == typeof(decimal) ||
                 type == typeof(DateTime) ||
                 type == typeof(DateTimeOffset) ||
                 type == typeof(DateOnly) ||
                 type == typeof(TimeOnly) ||
                 type == typeof(Guid));
    }
}