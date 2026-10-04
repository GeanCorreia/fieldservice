namespace FieldService.Shared.Configuration;

public class ApplicationAccountOptions
{
    public const string SectionName = "ApplicationAccount";
    public Guid ServiceUserId { get; set; } = Guid.Empty;
    public Guid TenantId { get; set; } = Guid.Empty;
}