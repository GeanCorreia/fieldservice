namespace FieldService.Authentication.Interfaces;

public interface IUserIdentityResolver
{
    Task ResolveUserAsync(CancellationToken cancellationToken = default);
    Task ResolveSessionAsync(CancellationToken cancellationToken = default);
}