using System.Text.Json;
using FieldService.Form.Dtos;
using FieldService.Form.Entities;
using FieldService.Form.Interfaces;

namespace FieldService.Form.Extensions;

internal static class FormIoSchemaExtension 
{
    public static FormSnapshot FromSchema(this FormDto formDto)
    {
        var schemaJson = formDto.SchemaJson;
        var title = TryGetString(schemaJson, "title");
        
        var name = TryGetString(schemaJson, "name") ?? TryGetString(schemaJson, "path");
        
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Não foi possível determinar o Slug do formulário. As propriedades 'name' ou 'path' devem estar presentes no Schema JSON.");
        }

        var slug = Entities.Form.Slugify(name);
        
        var description = TryGetString(schemaJson, "description") 
                          ?? TryGetString(schemaJson, "properties", "description");
        
        if (title == null)
        {
            throw new InvalidOperationException("Não foi possível determinar o título do formulário. A propriedade 'title' deve estar presente no Schema JSON.");
        }

        return new FormSnapshot(
            Slug: slug,
            Title: title,
            Description: description,
            SchemaJson: schemaJson);
    }

    public static FormGridFilter Filter(this FormDto formDto)
    {
        var schemaJson = formDto.SchemaJson;
        var fields = new List<FormField>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (schemaJson.ValueKind == JsonValueKind.Object && 
            schemaJson.TryGetProperty("components", out var rootComponents) && 
            rootComponents.ValueKind == JsonValueKind.Array)
        {
            TraverseComponents(rootComponents, fields, seenKeys);
        }
        else if (schemaJson.ValueKind == JsonValueKind.Array)
        {
            TraverseComponents(schemaJson, fields, seenKeys);
        }

        return new FormGridFilter(
            formDto.Id,
            fields
        );
    }

    private static readonly HashSet<string> IgnoredComponentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "htmlelement", "content", "panel", "columns", 
        "well", "table", "reCaptcha", "unknown"
    };

    private static void TraverseComponents(
        JsonElement componentsArray, 
        List<FormField> fields, 
        HashSet<string> seenKeys)
    {
        foreach (var component in componentsArray.EnumerateArray())
        {
            if (component.ValueKind != JsonValueKind.Object)
                continue;

            var type = TryGetString(component, "type");
            if (string.IsNullOrWhiteSpace(type) || IgnoredComponentTypes.Contains(type))
            {
                TraverseChildContainers(component, fields, seenKeys);
                continue;
            }

            var isTableView = TryGetBool(component, "tableView") ?? true;

            if (isTableView)
            {
                var dataType = ResolveFilterableDataType(type, component);

                if (dataType.HasValue)
                {
                    var key = TryGetString(component, "key");

                    if (!string.IsNullOrWhiteSpace(key) && seenKeys.Add(key))
                    {
                        // 1. Label humanizado para o frontend (usa 'key' se o 'label' estiver ausente)
                        var label = TryGetString(component, "label") ?? key;

                        // 2. Extração da máscara (Form.io pode usar 'inputMask', 'mask' ou a opção de 'currency')
                        var inputMask = TryGetString(component, "inputMask") 
                                        ?? TryGetString(component, "mask");

                        // 3. Placeholder visual para inputs mascarados/textos
                        var placeholder = TryGetString(component, "placeholder");

                        fields.Add(new FormField(
                            FieldName: key,
                            Label: label,
                            FieldType: dataType.Value,
                            InputMask: inputMask,
                            Placeholder: placeholder
                        ));
                    }
                }
            }

            TraverseChildContainers(component, fields, seenKeys);
        }
    }

    private static void TraverseChildContainers(
        JsonElement component, 
        List<FormField> fields, 
        HashSet<string> seenKeys)
    {
        if (component.TryGetProperty("components", out var childComponents) && 
            childComponents.ValueKind == JsonValueKind.Array)
        {
            TraverseComponents(childComponents, fields, seenKeys);
        }

        if (component.TryGetProperty("columns", out var columns) && 
            columns.ValueKind == JsonValueKind.Array)
        {
            foreach (var col in columns.EnumerateArray())
            {
                if (col.ValueKind == JsonValueKind.Object && 
                    col.TryGetProperty("components", out var colComponents) && 
                    colComponents.ValueKind == JsonValueKind.Array)
                {
                    TraverseComponents(colComponents, fields, seenKeys);
                }
            }
        }

        if (component.TryGetProperty("rows", out var rows) && 
            rows.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array) continue;
                foreach (var cell in row.EnumerateArray())
                {
                    if (cell.ValueKind == JsonValueKind.Object && 
                        cell.TryGetProperty("components", out var cellComponents) && 
                        cellComponents.ValueKind == JsonValueKind.Array)
                    {
                        TraverseComponents(cellComponents, fields, seenKeys);
                    }
                }
            }
        }
    }

    private static FormDataType? ResolveFilterableDataType(string type, JsonElement fieldNode)
    {
        var cleanType = type.Trim().ToLowerInvariant();

        if (cleanType == "select" && TryGetBool(fieldNode, "multiple") == true)
        {
            return null;
        }

        return cleanType switch
        {
            "textfield" or "textarea" or "email" or "password" or 
            "phonenumber" or "url" or "select" or "radio" => FormDataType.String,

            "number" or "currency" => FormDataType.Number,

            "checkbox" => FormDataType.Boolean,

            "datetime" or "day" or "time" => FormDataType.Date,

            _ => null
        };
    }

    private static bool? TryGetBool(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? TryGetString(JsonElement node, string propertyName, string? subProperty = null)
    {
        if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(propertyName, out var property))
            return null;

        if (subProperty != null)
        {
            return TryGetString(property, subProperty);
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }
}