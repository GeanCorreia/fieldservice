using FieldService.Shared.Types.Http;

namespace FieldService.Shared.Dtos;

public record PagedDto<TData>
    where TData : AbstractDto
{
    public required IReadOnlyCollection<TData> Items { get; init; }
    public required Pagination Pagination { get; init; }

}
