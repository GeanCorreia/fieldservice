using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Services;

internal class SupersetAuthService : ISupersetAuthService
{
    private readonly ISupersetApi _supersetApi;
    private readonly SupersetLoginRequest _supersetLoginRequest;
    
    
    public SupersetAuthService(
        ISupersetApi supersetApi, 
        IOptions<SupersetOptions> options)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        
        _supersetLoginRequest = new SupersetLoginRequest(options.Value.Username, options.Value.Password);
        
    }
    public async Task<string> GetAdminToken(
        string fqdnUrl, 
        CancellationToken cancellationToken = default)
    {
        SupersetLoginResponse response = await _supersetApi.LoginAsync(
            new Uri(fqdnUrl), 
            _supersetLoginRequest, 
            cancellationToken);
        
        return response.access_token;
        
    }
}