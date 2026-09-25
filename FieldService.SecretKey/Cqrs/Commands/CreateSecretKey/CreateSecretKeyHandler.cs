using FieldService.SecretKey.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;

internal class CreateSecretKeyHandler : IRequestHandler<CreateSecretKeyCommand>
{
    private readonly ILogger<CreateSecretKeyHandler> _logger;
    private readonly ISecretKeyService _secretKeyService;
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    
    public CreateSecretKeyHandler(ILogger<CreateSecretKeyHandler> logger, ISecretKeyService secretKeyService, ISecretKeyVaultService secretKeyVaultService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretKeyService = secretKeyService ?? throw new ArgumentNullException(nameof(secretKeyService));
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }

    public async Task Handle(
        CreateSecretKeyCommand request, 
        CancellationToken cancellationToken)
    {
        
        var secretKey = Entities.SecretKey.Create(
            request.SecretKeyDto.Secret,
            request.SecretKeyDto.Reference.Name,
            request.SecretKeyDto.Reference.TenantId,
            request.UserId
            );
        
        await _secretKeyService.SaveSecretKeyAsync(secretKey, cancellationToken);

        try
        {
            await _secretKeyVaultService.SetSecretKeyAsync(
                request.SecretKeyDto.Reference.TenantId, 
                request.SecretKeyDto.Reference.Name, 
                request.SecretKeyDto.Secret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret key {SecretName} in vault for tenant {TenantId}", 
                request.SecretKeyDto.Reference.Name, 
                request.SecretKeyDto.Reference.TenantId);
        }
    }
}