using System.Security.Claims;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionManager
{
    Task TouchAsync(CancellationToken cancellationToken = default);
    
}