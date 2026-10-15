using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetDeletedFormGridData;

public record GetDeletedFormGridDataQuery(
    Guid TenantId,
    Guid FormId) : IRequest<FormGridDataDto>;

