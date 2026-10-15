using FieldService.Form.Interfaces;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.UpdateFormSubmission;

internal class UpdateFormSubmissionHandler : IRequestHandler<UpdateFormSubmissionCommand>
{
    private readonly IFormSubmissionService _formSubmissionService;

    public UpdateFormSubmissionHandler(IFormSubmissionService formSubmissionService)
    {
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
    }

    public async Task Handle(UpdateFormSubmissionCommand request, CancellationToken cancellationToken)
    {
        await _formSubmissionService.UpdateAsync(
            request.TenantId,
            request.SubmissionId,
            request.UserId,
            request.DataJson,
            cancellationToken);
    }
}

