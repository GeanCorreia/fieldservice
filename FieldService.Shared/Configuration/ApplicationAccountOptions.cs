namespace FieldService.Shared.Configuration;

public class ApplicationAccountOptions
{
    public const string SectionName = "ApplicationAccount";
    public string[] AllowedFrameAncestors { get; set; } = Array.Empty<string>();
    public Guid ServiceUserId { get; set; } = Guid.Empty;
    public Guid TenantId { get; set; } = Guid.Empty;
    public string TenantName { get; set; } = string.Empty;
}