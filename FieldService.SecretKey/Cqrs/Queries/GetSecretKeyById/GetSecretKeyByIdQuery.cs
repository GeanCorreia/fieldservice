using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;

public record GetSecretKeyByIdQuery<T>(Guid SecretKeyId) : IRequest<T?>
	where T : class, ISecretKeyType;
