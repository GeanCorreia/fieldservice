using System.Security.Claims;
using FieldService.Authentication.Entities;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Interfaces;

public interface IIdentityProvider
{

    Task<string> CreateUserAsync(
        Guid userId,
        Email email,
        string name);

    Task DisableUserAsync(
        string externalUserId);

    Task EnableUserAsync(
        string externalUserId);

    Task DeleteUserAsync(
        string externalUserId);
    
}