using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormGridData;

public record GetFormGridDataQuery(
    Guid TenantId,
    FormGridFilterRequest Filter
    ) : IRequest<FormGridDataDto>;