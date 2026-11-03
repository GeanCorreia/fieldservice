using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetJobTitleById;

internal sealed record GetJobTitleByIdQuery(Guid TenantId, Guid JobTitleId) : IRequest<JobTitleDto?>;

