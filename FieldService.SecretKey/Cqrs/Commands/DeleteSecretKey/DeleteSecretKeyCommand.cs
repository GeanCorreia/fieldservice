

using MediatR;

namespace FieldService.SecretKey.Cqrs.Commands.DeleteSecretKey;

public record DeleteSecretKeyCommand(
    string SecretName,
    Guid TenantId,
    Guid UserId): IRequest;