using MediatR;

namespace FieldService.Form.Cqrs.Commands.DeleteForm;

public record DeleteFormCommand(
    Guid TenantId,
    Guid FormId,
    Guid UserId
    ) : IRequest;