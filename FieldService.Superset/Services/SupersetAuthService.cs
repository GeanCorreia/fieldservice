using System.Security.Cryptography;
using FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Entities;
using FieldService.Shared.Services;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FieldService.Superset.Services;

internal class SupersetAuthService : ISupersetAuthService
{
    private readonly string _connectionString;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly SupersetLoginRequest _supersetLoginRequest;
    private readonly SupersetOptions _supersetOptions;
    private readonly string _host;
    private readonly int _port;
    
    
    public SupersetAuthService(
        IMediator mediator,
        ISupersetApi supersetApi, 
        IConfiguration configuration,
        IOptions<SupersetOptions> supersetOptions)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        if (supersetOptions == null)
        {
            throw new ArgumentNullException(nameof(supersetOptions));
        }
        _connectionString = configuration.GetConnectionString("SupersetHostDatabase")
                            ?? throw new NullReferenceException("Connection string 'SupersetHostDatabase' not found in configuration.");
        
        _supersetLoginRequest = new SupersetLoginRequest(supersetOptions.Value.Username, supersetOptions.Value.Password);
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _host = _supersetOptions.DataBaseHost.Host;
        _port = _supersetOptions.DataBaseHost.Port;
    }
    public async Task<string> GetAdminTokenApi(
        string fqdnUrl, 
        CancellationToken cancellationToken = default)
    {
        SupersetLoginResponse response = await _supersetApi.LoginAsync(
            new Uri(fqdnUrl), 
            _supersetLoginRequest, 
            cancellationToken);
        
        return response.access_token;
        
    }

    async Task<string> ISupersetAuthService.GetDatabaseConnectionString(
        Guid connectionStringId, 
        CancellationToken cancellationToken)
    {
        var connectionString = await _mediator.Send(new GetSecretKeyByIdQuery<ConnectionStringSecret>(connectionStringId), cancellationToken);
        if(connectionString == null)
        {
            throw new KeyNotFoundException($"Connection string with ID '{connectionStringId}' was not found.");
        }
        
        return connectionString.ConnectionString;
    }

    public async Task<string> GetSupersetSecretApiKey(
        Guid apiKeyId, 
        CancellationToken cancellationToken = default)
    {
        var secret = await _mediator.Send(new GetSecretKeyByIdQuery<ApiKeySecret>(apiKeyId), cancellationToken);
        if(secret == null)
        {
            throw new KeyNotFoundException($"API key with ID '{apiKeyId}' was not found.");
        }
        
        return secret.ApiKey;
    }

    public async Task<(Guid KeyId, string Key)> CreateSupersetSecretApiKey(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        
        var reference = new SecretKeyReferenceDto(
            id,
            SecretKeyType.WebhookSigningSecret,
            tenantId,
            SupersetTenantConfig.SecretKeyName(tenantId),
            false
        );

        var key = new WebhookSigningSecret(
            CreateSecretKeyService.CreateSecretKey(),
            HashAlgorithmType.HmacSha256
        );
        
        var dto = new SecretKeyDto(
            reference,
            key);
        
        await _mediator.Send(new CreateSecretKeyCommand( userId, dto), cancellationToken);

        return (id, key.SecretKey);

    }
    public async Task<(Guid ConnectionStringId, SupersetDatabaseParams databaseParams, string ConnectionString)> CreateDatabaseConnectionString(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        
        var reference = new SecretKeyReferenceDto(
            id,
            SecretKeyType.ConnectionString,
            tenantId,
            SupersetTenantConfig.ConnectionStringName(tenantId),
            false
        );

        var userName = $"user_{tenantId.ToString().Replace("-", string.Empty)[..8]}";
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var databaseName = SupersetTenantConfig.Database(tenantId);
        
        var databaseParams = new SupersetDatabaseParams(
            userName,
            password,
            databaseName);
        
        var connectionString = BuildSupersetMetadataConnectionString(databaseParams);
        var key = new ConnectionStringSecret(
            connectionString);
        
        var dto = new SecretKeyDto(
            reference,
            key);
        
        await _mediator.Send(new CreateSecretKeyCommand( userId, dto), cancellationToken);

        return (id, databaseParams, connectionString);
    }
    
    private string BuildSupersetMetadataConnectionString(SupersetDatabaseParams databaseParams)
    {
        
        return new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = databaseParams.Username,
            Host = _host,
            Port = _port
        }.ConnectionString;
        
        
    }
    
}