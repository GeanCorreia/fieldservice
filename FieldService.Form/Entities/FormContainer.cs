using FieldService.Shared.Types;

namespace FieldService.Form.Entities;

public class FormContainer
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid SecretKeyId { get; init; }
    public CloudProvider CloudProvider { get; private set; }
}