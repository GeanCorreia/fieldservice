using FieldService.SecretKey.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldService.SecretKey.Cqrs.Commands.UpdateSecretKey;

internal class UpdateSecretKeyHandler : IRequestHandler<UpdateSecretKeyCommand>
{
    private readonly ISecretKeyRepository _secretKeyRepository;
    private readonly ILogger<UpdateSecretKeyHandler> _logger;
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    
    public UpdateSecretKeyHandler(
        ISecretKeyRepository secretKeyRepository, 
        ILogger<UpdateSecretKeyHandler> logger,
        ISecretKeyVaultService secretKeyVaultService)
    {
        _secretKeyRepository = secretKeyRepository ?? throw new ArgumentNullException(nameof(secretKeyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }
    
    public async Task Handle(
        UpdateSecretKeyCommand request, 
        CancellationToken cancellationToken)
    {
        var secretKey = await _secretKeyRepository.GetSecretKeyAsync(
            request.TenantId, 
            request.SecretName, 
            cancellationToken);

        if (secretKey == null)
        {
            throw new KeyNotFoundException($"Secret key '{request.SecretName}' not found for tenant '{request.TenantId}'.");
        }
        
        if(string.IsNullOrEmpty(request.NewSecretName) && !request.NewSecretKeyType.HasValue)
        {
            throw new InvalidOperationException("At least one of NewSecretName or NewSecretKeyType must be provided for update.");
        }
        
        if(request.NewSecretName != null)
        {
            secretKey.UpdateName(request.NewSecretName, request.UserId);
        }
        
        if(request.NewSecretKeyType.HasValue)
        {
            secretKey.UpdateType(request.UserId, request.NewSecretKeyType.Value);
        }
        
        await _secretKeyRepository.SaveSecretKeyAsync(secretKey, cancellationToken);

        try
        {
            await _secretKeyVaultService.RemoveSecretKeyAsync(request.TenantId, request.SecretName);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update secret key {SecretName} in vault for tenant {TenantId}", 
                request.SecretName, 
                request.TenantId);
            
        }        
        
    }
    
}