using MediatR;

namespace FieldService.Form.Cqrs.Commands.DeleteFormSubmission;

public record DeleteFormSubmissionCommand(
    Guid TenantId,
    Guid SubmissionId,
    Guid UserId) : IRequest;

