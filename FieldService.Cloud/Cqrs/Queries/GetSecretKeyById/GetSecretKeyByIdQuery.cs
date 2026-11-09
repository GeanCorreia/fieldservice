using FieldService.Cloud.Interfaces;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKeyById;

public record GetSecretKeyByIdQuery<T>(Guid SecretKeyId) : IRequest<T?>
	where T : class, ISecretKeyType;
