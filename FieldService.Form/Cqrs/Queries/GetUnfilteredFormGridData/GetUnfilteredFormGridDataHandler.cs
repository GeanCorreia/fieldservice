using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetUnfilteredFormGridData;

internal class GetUnfilteredFormGridDataHandler : IRequestHandler<GetUnfilteredFormGridDataQuery, FormGridDataDto>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public GetUnfilteredFormGridDataHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task<FormGridDataDto> Handle(GetUnfilteredFormGridDataQuery request, CancellationToken cancellationToken)
    {
        var filterRequest = new FormGridFilterRequest(
            TenantId: request.TenantId,
            FormId: request.FormId,
            Filters: [],
            PagedRequest: request.PagedRequest);

        return await _formSubmissionService.GetFormGridData(filterRequest, cancellationToken);
    }
}

