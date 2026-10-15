using System.Text.Json;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.CreateFormSubmission;

public record CreateFormSubmissionCommand(
    Guid TenantId,
    Guid FormId,
    Guid UserId,
    JsonElement DataJson) : IRequest;

