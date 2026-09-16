namespace FieldService.Shared.Types;

public static class ClaimsExtensions
{
    // Authentication
    public const string AuthenticationIdentity = "Authentication";
    public const string UserId = "Authentication.UserId";
    public const string TenantId = "Authentication.TenantId";
    public const string SessionId = "Authentication.SessionId";
    public const string Provider = "Authentication.Provider";


    // Authorization
    public const string AuthorizationIdentity = "Authorization";
    
    public const string Role = "Authorization.Role";
    public const string Permissions = "Authorization.Permissions";
}
