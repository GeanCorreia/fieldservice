namespace FieldService.Authentication.Interfaces;

public interface IUserIdentityResolver
{
    Task ResolveUserAsync(CancellationToken cancellationToken = default);
}