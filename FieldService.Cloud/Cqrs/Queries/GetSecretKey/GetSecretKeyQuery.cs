using FieldService.Cloud.Interfaces;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKey;

public record GetSecretKeyQuery<T>(
    Guid TenantId, 
    string SecretName) : IRequest<T>
    where T : class, ISecretKeyType;
