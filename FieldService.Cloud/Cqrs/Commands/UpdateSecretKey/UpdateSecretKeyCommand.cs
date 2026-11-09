using FieldService.Cloud.Entities;
using MediatR;

namespace FieldService.Cloud.Cqrs.Commands.UpdateSecretKey;

public record UpdateSecretKeyCommand(
    Guid UserId,
    Guid TenantId,
    string SecretName,
    SecretKeyType SecretKeyType,
    string? NewSecretName = null,
    SecretKeyType? NewSecretKeyType = null) : IRequest;