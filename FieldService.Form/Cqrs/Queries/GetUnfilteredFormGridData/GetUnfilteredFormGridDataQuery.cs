using FieldService.Form.Dtos;
using FieldService.Shared.Types.Http;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetUnfilteredFormGridData;

public record GetUnfilteredFormGridDataQuery(
    Guid TenantId,
    Guid FormId,
    PagedRequest? PagedRequest = null) : IRequest<FormGridDataDto>;

