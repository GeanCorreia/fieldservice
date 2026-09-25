using FieldService.SecretKey.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.SecretKey.Cqrs.Commands.DeleteSecretKey;

internal class DeleteSecretKeyHandler : IRequestHandler<DeleteSecretKeyCommand>
{
    private readonly ISecretKeyService _secretKeyService;
    private readonly ILogger<DeleteSecretKeyHandler> _logger;
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    
    
    public DeleteSecretKeyHandler(
        ISecretKeyService secretKeyService, 
        ILogger<DeleteSecretKeyHandler> logger,
        ISecretKeyVaultService secretKeyVaultService)
    {
        _secretKeyService = secretKeyService ?? throw new ArgumentNullException(nameof(secretKeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }
    
    public async Task Handle(
        DeleteSecretKeyCommand request, 
        CancellationToken cancellationToken)
    {
        var secretKey = await _secretKeyService.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secretKey == null)
        {
            _logger.LogWarning("Secret key {SecretName} not found for tenant {TenantId}", request.SecretName, request.TenantId);
            return;
        }

        secretKey.Delete(request.UserId);
        await _secretKeyService.SaveSecretKeyAsync(secretKey, cancellationToken);
        try
        {
            await _secretKeyVaultService.RemoveSecretKeyAsync(request.TenantId, request.SecretName);
             await _secretKeyService.RemoveSecretKeyAsync(secretKey.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove secret key {SecretName} from vault for tenant {TenantId}", request.SecretName, request.TenantId);
            
        }
        
    }
}