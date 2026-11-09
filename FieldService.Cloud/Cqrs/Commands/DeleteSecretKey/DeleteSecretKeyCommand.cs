

using MediatR;

namespace FieldService.Cloud.Cqrs.Commands.DeleteSecretKey;

public record DeleteSecretKeyCommand(
    string SecretName,
    Guid TenantId,
    Guid UserId): IRequest;