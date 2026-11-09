using FieldService.Cloud.Dtos;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKeyReferenceAt;

public record GetSecretKeyReferenceAtQuery(
    Guid TenantId, 
    string SecretName, 
    Guid UserId,
    DateTimeOffset? ReferenceDate = null) : IRequest<SecretKeyHistoryDto>;