using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetForms;

internal class GetFormsHandler : IRequestHandler<GetFormsQuery, IEnumerable<FormDto>>
{
    private readonly IFormService _formService;
    
    public GetFormsHandler(IFormService formService)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task<IEnumerable<FormDto>> Handle(GetFormsQuery request, CancellationToken cancellationToken)
    {
        return await _formService.GetFormsAsync(request.TenantId, cancellationToken);
    }
}