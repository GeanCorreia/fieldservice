using FieldService.Form.Dtos;
using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormInfos;

internal class GetFormInfosHandler : IRequestHandler<GetFormInfosQuery, IEnumerable<FormInfoDto>>
{
    private readonly IMediator _mediator;
    private readonly IFormService _formService;

    public GetFormInfosHandler(IMediator mediator, IFormService formService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task<IEnumerable<FormInfoDto>> Handle(GetFormInfosQuery request, CancellationToken cancellationToken)
    {
        return await _formService.GetFormInfosByTenantAsync(request.TenantId, cancellationToken);
    }
}