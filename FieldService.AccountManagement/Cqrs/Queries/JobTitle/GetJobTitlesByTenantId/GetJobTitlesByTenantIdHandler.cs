using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetJobTitlesByTenantId;

internal sealed class GetJobTitlesByTenantIdHandler(IAccountManagementService service)
    : IRequestHandler<GetJobTitlesByTenantIdQuery, IEnumerable<JobTitleDto>>
{
    public Task<IEnumerable<JobTitleDto>> Handle(
        GetJobTitlesByTenantIdQuery request,
        CancellationToken cancellationToken) =>
        service.GetJobTitlesByTenantIdAsync(request.TenantId, cancellationToken);
}

