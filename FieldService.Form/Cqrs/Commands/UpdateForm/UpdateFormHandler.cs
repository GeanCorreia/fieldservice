using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.UpdateForm;

internal class UpdateFormHandler : IRequestHandler<UpdateFormCommand>
{
    private readonly IFormService _formService;

    public UpdateFormHandler(IFormService formService)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task Handle(UpdateFormCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId != request.Form.TenantId)
        {
            throw new UnauthorizedAccessException("Tenant ID does not match the form tenant.");
        }

        var forms = await _formService.GetFormsAsync(request.TenantId, cancellationToken);
        
        if (!forms.Any(f => f.Id == request.Form.Id))
        {
            throw new InvalidOperationException($"Form with ID '{request.Form.Id}' does not exist.");
        }
        
        if(forms.Any(f => f.Slug == request.Form.Slug && f.Id != request.Form.Id))
        {
            throw new InvalidOperationException($"Form with slug '{request.Form.Slug}' already exists " +
                                                $"for tenant '{request.TenantId}'.");
        }
        
        await _formService.UpdateFormAsync(request.Form, request.UserId, cancellationToken);
    }
}