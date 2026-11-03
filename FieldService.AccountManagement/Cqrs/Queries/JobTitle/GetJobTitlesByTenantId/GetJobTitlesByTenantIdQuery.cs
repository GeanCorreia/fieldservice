using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetJobTitlesByTenantId;

internal sealed record GetJobTitlesByTenantIdQuery(Guid TenantId) : IRequest<IEnumerable<JobTitleDto>>;

