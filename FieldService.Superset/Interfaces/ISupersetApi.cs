using Refit;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;


internal interface ISupersetApi
{

    [Post("/api/v1/security/login")]
    Task<SupersetLoginResponse> LoginAsync(
        [Body] SupersetLoginRequest request, 
        CancellationToken ct = default
    );
    
    [Post("/api/v1/security/guest_token/")]
    Task<SupersetGuestTokenResponse> GetGuestTokenAsync(
        [Header("Authorization")] string bearerToken, 
        [Body] SupersetGuestTokenRequest request, 
        CancellationToken ct = default
    );
}