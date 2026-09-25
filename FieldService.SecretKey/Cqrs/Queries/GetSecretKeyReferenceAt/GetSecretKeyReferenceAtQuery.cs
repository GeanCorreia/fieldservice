using FieldService.SecretKey.Dtos;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyReferenceAt;

public record GetSecretKeyReferenceAtQuery(
    Guid TenantId, 
    string SecretName, 
    Guid UserId,
    DateTimeOffset? ReferenceDate = null) : IRequest<SecretKeyHistoryDto>;