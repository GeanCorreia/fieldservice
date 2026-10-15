using System.Text.Json;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Form.Dtos;

public record FormDto : AbstractDto
{
    private readonly bool _isActive;

    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
    public override bool IsActive => _isActive;
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
        _isActive = isActive;
        Description = description;
        
    }

    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(FormDto);
    public override string? ResourceId => Id.ToString();
    
    
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