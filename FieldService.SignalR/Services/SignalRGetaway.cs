using System.Security.Claims;
using FieldService.Authorization.Interfaces;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Services;

internal sealed class SignalRGetaway(ITokenValidator tokenValidator) : ISignalRGetaway
{
    public async Task<SignalRConnectionContext> ConnectAsync(
        string jwtToken,
        string connectionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            throw new ArgumentException("JWT token is required.", nameof(jwtToken));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        var user = await tokenValidator.ValidateJwtAsync(jwtToken, ct);
        var userId = TryGetGuid(user, ClaimTypes.NameIdentifier, "sub", "user_id");
        var deviceId = TryGetGuid(user, "device_id", "did");
        var tenantIds = GetGuidClaims(user, "tenant_id", "tid", "tenants");

        if (!userId.HasValue)
            throw new InvalidOperationException("JWT does not contain a valid user id claim.");
        if (!deviceId.HasValue)
            throw new InvalidOperationException("JWT does not contain a valid device id claim.");
        if (tenantIds.Count == 0)
            throw new InvalidOperationException("JWT does not contain any valid tenant id claim.");

        return new SignalRConnectionContext(
            connectionId,
            user,
            userId.Value,
            deviceId.Value,
            tenantIds);
    }

    private static Guid? TryGetGuid(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (Guid.TryParse(value, out var guid))
                return guid;
        }

        return null;
    }

    private static IReadOnlyCollection<Guid> GetGuidClaims(ClaimsPrincipal user, params string[] claimTypes)
    {
        var values = user.Claims
            .Where(c => claimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .SelectMany(SplitValues)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var tenantIds = new List<Guid>();
        foreach (var value in values)
        {
            if (Guid.TryParse(value, out var guid))
                tenantIds.Add(guid);
        }

        return tenantIds.Distinct().ToArray();
    }

    private static IEnumerable<string> SplitValues(string value)
    {
        return value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
