using System.Security.Claims;
using FieldService.Shared.Types;

namespace FieldService.Shared.Interfaces;

public interface IRequestContextManager
{
    RequestContext Request { get; }
    ClaimsPrincipal Principal { get; }
    void Initialize(
        RequestContext requestContext);
    void SetPrincipal(ClaimsPrincipal principal);
    void Clear();
}