using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKey;

public record GetSecretKeyQuery<T>(
    Guid TenantId, 
    string SecretName) : IRequest<T?> 
    where T : ISecretKeyType;