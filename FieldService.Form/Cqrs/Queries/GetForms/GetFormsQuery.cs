using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetForms;

public record GetFormsQuery(
    Guid TenantId) : IRequest<IEnumerable<FormDto>>;