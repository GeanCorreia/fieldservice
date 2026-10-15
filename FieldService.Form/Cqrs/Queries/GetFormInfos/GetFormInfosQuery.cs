using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormInfos;


public record GetFormInfosQuery(
    Guid TenantId) : IRequest<IEnumerable<FormInfoDto>>;