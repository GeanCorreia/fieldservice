using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Commands.UpdateForm;

public record UpdateFormCommand(
    Guid TenantId,
    FormDto Form,
    Guid UserId) : IRequest;