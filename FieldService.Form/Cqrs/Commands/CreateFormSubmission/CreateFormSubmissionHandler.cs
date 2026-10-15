using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.CreateFormSubmission;

internal class CreateFormSubmissionHandler : IRequestHandler<CreateFormSubmissionCommand>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public CreateFormSubmissionHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task Handle(CreateFormSubmissionCommand request, CancellationToken cancellationToken)
    {
        await _formSubmissionService.CreateAsync(
            request.TenantId,
            request.FormId,
            request.UserId,
            request.DataJson,
            cancellationToken);
    }
}

