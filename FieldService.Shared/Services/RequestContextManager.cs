using System.Security.Claims;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Shared.Services;

public sealed class RequestContextManager 
    : IRequestContextManager
{
    private RequestContext? _request;
    private ClaimsPrincipal? _principal;

    public RequestContext Request =>
        _request ?? throw new InvalidOperationException(
            "RequestContext was not initialized.");

    public ClaimsPrincipal Principal =>
        _principal ?? throw new InvalidOperationException(
            "ClaimsPrincipal was not initialized.");

    public void SetPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        _principal = principal;
    }
    public void Initialize(
        RequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        if (_request is not null)
            throw new InvalidOperationException(
                "RequestContext already initialized.");

        _request = requestContext;

    }


    public void Clear()
    {
        _request = null;
        _principal = null;
    }

}