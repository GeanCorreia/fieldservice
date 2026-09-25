using FieldService.SecretKey.Dtos;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyHistory;

public record GetSecretKeyHistoryQuery(
    Guid TenantId, 
    string SecretName,
    Guid UserId) : IRequest<IEnumerable<SecretKeyHistoryDto>>;