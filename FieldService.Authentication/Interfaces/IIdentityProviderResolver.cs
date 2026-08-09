namespace FieldService.Authentication.Interfaces;

public interface IIdentityProviderResolver
{
    IIdentityProvider Resolve(string token);
}