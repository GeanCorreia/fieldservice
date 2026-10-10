using FieldService.Shared.Types;

namespace FieldService.Shared.Types.Http;

public record PagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool IsDescending = false)
{
    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize switch
    {
        < 1 => 10,
        > 100 => 100, 
        _ => PageSize
    };
};
