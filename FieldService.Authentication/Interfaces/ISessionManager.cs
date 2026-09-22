using System.Security.Claims;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Interfaces;

public interface ISessionManager
{
    Task TouchAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
    
}