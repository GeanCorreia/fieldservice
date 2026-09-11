using FieldService.Shared.Responses;
using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public record PagedDto<TData>
    where TData : AbstractDto
{
    public required IReadOnlyCollection<TData> Items { get; init; }
    public required PaginationResponse Pagination { get; init; }

}
