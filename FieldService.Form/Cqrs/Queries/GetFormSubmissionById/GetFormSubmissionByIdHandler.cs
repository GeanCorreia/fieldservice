using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormSubmissionById;

internal class GetFormSubmissionByIdHandler : IRequestHandler<GetFormSubmissionByIdQuery, FormSubmissionDto?>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public GetFormSubmissionByIdHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task<FormSubmissionDto?> Handle(GetFormSubmissionByIdQuery request, CancellationToken cancellationToken)
    {
        var submission = await _formSubmissionService.GetByIdAsync(request.TenantId, request.SubmissionId, cancellationToken);
        return submission?.ToDto();
    }
}

