namespace FieldService.Authentication.Types;

public sealed class AuthenticationSessionOptions
{
    public int InactivityTimeoutInMinutes { get; init; } = 30;
    public int TokenLifetimeInMinutes { get; init; } = 60;
    public int CacheTtlExtraHours { get; init; } = 2;
    public int PersistenceInactivityThresholdInMinutes { get; init; } = 30;
}
