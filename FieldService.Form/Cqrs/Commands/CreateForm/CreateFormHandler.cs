using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.CreateForm;

internal class CreateFormHandler : IRequestHandler<CreateFormCommand>
{
    private readonly IFormService _formService;

    public CreateFormHandler(IFormService formService)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task Handle(CreateFormCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId != request.Form.TenantId)
        {
            throw new UnauthorizedAccessException("Tenant ID does not match the form tenant.");
        }

        var forms = await _formService.GetFormsAsync(request.TenantId, cancellationToken);
        
        if (forms.Any(f => f.Slug == request.Form.Slug))
        {
            throw new InvalidOperationException($"Form with slug '{request.Form.Slug}' already exists for tenant '{request.TenantId}'.");
        }
        
        await _formService.CreateFormAsync(request.Form, request.UserId, cancellationToken);
    }
    
}