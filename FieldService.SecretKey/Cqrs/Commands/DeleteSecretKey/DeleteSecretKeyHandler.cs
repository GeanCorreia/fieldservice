using FieldService.SecretKey.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.SecretKey.Cqrs.Commands.DeleteSecretKey;

internal class DeleteSecretKeyHandler : IRequestHandler<DeleteSecretKeyCommand>
{
    private readonly ISecretKeyRepository _repository;
    private readonly ILogger<DeleteSecretKeyHandler> _logger;
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    
    
    public DeleteSecretKeyHandler(
        ISecretKeyRepository repository, 
        ILogger<DeleteSecretKeyHandler> logger,
        ISecretKeyVaultService secretKeyVaultService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }
    
    public async Task Handle(
        DeleteSecretKeyCommand request, 
        CancellationToken cancellationToken)
    {
        var secretKey = await _repository.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secretKey == null)
        {
            _logger.LogWarning("Secret key {SecretName} not found for tenant {TenantId}", request.SecretName, request.TenantId);
            return;
        }

        secretKey.Delete(request.UserId);
        await _repository.SaveSecretKeyAsync(secretKey, cancellationToken);
        try
        {
            await _secretKeyVaultService.RemoveSecretKeyAsync(request.TenantId, request.SecretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove secret key {SecretName} from vault for tenant {TenantId}", request.SecretName, request.TenantId);
            
        }
        
    }
}