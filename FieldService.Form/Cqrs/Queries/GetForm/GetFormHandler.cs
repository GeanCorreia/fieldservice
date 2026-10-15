using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetForm;

internal class GetFormHandler : IRequestHandler<GetFormQuery, FormDto?>
{
    
    private readonly IMediator _mediator;
    private readonly IFormService _formService;
    
    public GetFormHandler(IMediator mediator, IFormService formService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task<FormDto?> Handle(GetFormQuery request, CancellationToken cancellationToken)
    {
        return await _formService.GetFormDtoAsync(request.FormId, request.TenantId, cancellationToken);
    }
}