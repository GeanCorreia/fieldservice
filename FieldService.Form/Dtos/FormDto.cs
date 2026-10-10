using System.Text.Json;

namespace FieldService.Form.Dtos;

public record FormDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
    public JsonElement SchemaJson { get; init; }

    public FormDto(
        Guid id, 
        Guid tenantId, 
        string slug, 
        string title, 
        JsonElement schemaJson,
        bool isActive,
        string? description = null)
    {
        Id = id;
        TenantId = tenantId;
        Slug = slug;
        Title = title;
        SchemaJson = schemaJson;
        IsActive = isActive;
        Description = description;
        
    }
    
    
}


internal static class FormDtoExtensions
{
    public static FormDto ToDto(this Entities.Form form)
    {
        return new FormDto(
            id: form.Id,
            tenantId: form.TenantId,
            slug: form.Slug,
            title: form.Title,
            schemaJson: form.SchemaJson,
            isActive: form.IsActive,
            description: form.Description
        );
    }
}