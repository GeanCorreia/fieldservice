using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.DeleteFormSubmission;

internal class DeleteFormSubmissionHandler : IRequestHandler<DeleteFormSubmissionCommand>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public DeleteFormSubmissionHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task Handle(DeleteFormSubmissionCommand request, CancellationToken cancellationToken)
    {
        await _formSubmissionService.DeleteAsync(
            request.TenantId,
            request.SubmissionId,
            request.UserId,
            cancellationToken);
    }
}

