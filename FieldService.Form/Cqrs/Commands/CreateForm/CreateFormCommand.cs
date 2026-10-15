using FieldService.Form.Dtos;
using MediatR;
namespace FieldService.Form.Cqrs.Commands.CreateForm;

public record CreateFormCommand(
    Guid TenantId,
    FormDto Form,
    Guid UserId
) : IRequest;