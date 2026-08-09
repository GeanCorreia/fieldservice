using FieldService.Authentication.Interfaces;

namespace FieldService.Authentication.SessionAttribute;

public class SessionAtributes
{
    public sealed class SessionCreationAttribute : Attribute, ISessionOperation { }
    public sealed class TenantSelectionAttribute : Attribute, ISessionOperation { }
}