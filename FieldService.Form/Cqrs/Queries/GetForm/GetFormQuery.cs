using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetForm;

public record GetFormQuery(
    Guid TenantId,
    Guid FormId) : IRequest<FormDto?>;