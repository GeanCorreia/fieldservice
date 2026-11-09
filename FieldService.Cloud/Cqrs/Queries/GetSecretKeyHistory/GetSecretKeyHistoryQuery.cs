using FieldService.Cloud.Dtos;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKeyHistory;

public record GetSecretKeyHistoryQuery(
    Guid TenantId, 
    string SecretName,
    Guid UserId) : IRequest<IEnumerable<SecretKeyHistoryDto>>;