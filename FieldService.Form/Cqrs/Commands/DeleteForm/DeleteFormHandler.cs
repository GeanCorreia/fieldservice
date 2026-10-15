using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.DeleteForm;

internal class DeleteFormHandler : IRequestHandler<DeleteFormCommand>
{
    private readonly IFormService _formService;

    public DeleteFormHandler(IFormService formService)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    }

    public async Task Handle(DeleteFormCommand request, CancellationToken cancellationToken)
    {
        var forms = await _formService.GetFormsAsync(request.TenantId, cancellationToken);

        if (!forms.Any(f => f.Id == request.FormId))
        {
            throw new InvalidOperationException($"Form with ID '{request.FormId}' does not exist.");
        }

        await _formService.DeleteFormAsync(request.FormId, request.UserId, cancellationToken);
    }
}