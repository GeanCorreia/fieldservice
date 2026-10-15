using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetDeletedFormGridData;

internal class GetDeletedFormGridDataHandler : IRequestHandler<GetDeletedFormGridDataQuery, FormGridDataDto>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public GetDeletedFormGridDataHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task<FormGridDataDto> Handle(GetDeletedFormGridDataQuery request, CancellationToken cancellationToken)
    {
        return await _formSubmissionService.GetAllDeletedAsync(
            request.TenantId,
            request.FormId,
            cancellationToken);
    }
}

