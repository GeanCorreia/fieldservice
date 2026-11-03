using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantReferenceById;

public sealed record GetTenantReferenceByIdQuery(Guid TenantId) : IRequest<TenantReferenceDto?>;

