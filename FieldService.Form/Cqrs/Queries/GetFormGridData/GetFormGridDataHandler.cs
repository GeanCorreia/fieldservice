using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormGridData;

internal class GetFormGridDataHandler : IRequestHandler<GetFormGridDataQuery, FormGridDataDto>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public GetFormGridDataHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task<FormGridDataDto> Handle(GetFormGridDataQuery request, CancellationToken cancellationToken)
    {
        if(request.TenantId != request.Filter.TenantId)
        {
            throw new UnauthorizedAccessException("Tenant ID does not match the filter.");
        }

        return await _formSubmissionService.GetFilteredFormGridData(request.Filter, cancellationToken);
    }
}