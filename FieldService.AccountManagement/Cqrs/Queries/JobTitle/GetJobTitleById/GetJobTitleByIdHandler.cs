using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetJobTitleById;

internal sealed class GetJobTitleByIdHandler(IAccountManagementService service)
    : IRequestHandler<GetJobTitleByIdQuery, JobTitleDto?>
{
    public Task<JobTitleDto?> Handle(GetJobTitleByIdQuery request, CancellationToken cancellationToken) =>
        service.GetJobTitleByIdAsync(request.TenantId, request.JobTitleId, cancellationToken);
}

