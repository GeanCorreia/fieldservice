using System.Text.Json;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.UpdateFormSubmission;

public record UpdateFormSubmissionCommand(
    Guid TenantId,
    Guid SubmissionId,
    Guid UserId,
    JsonElement DataJson) : IRequest;

