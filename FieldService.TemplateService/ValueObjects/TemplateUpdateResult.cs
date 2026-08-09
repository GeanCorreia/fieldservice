using FieldService.TemplateService.Entities;
using FieldService.TemplateService.Enums;

namespace FieldService.Shared.Types;

public record TemplateUpdateResult(
    VersionChangeType ChangeType,
    SchemaVersion SchemaVersion,
    TemplateVersion TemplateVersion);
