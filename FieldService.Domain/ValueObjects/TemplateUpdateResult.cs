using FieldService.Domain.Entities;
using FieldService.Domain.Enums;

namespace FieldService.Domain.ValueObjects;

public record TemplateUpdateResult(
    VersionChangeType ChangeType,
    Version Version,
    TemplateVersion TemplateVersion);
