namespace FieldService.Shared.Responses;

public record Pagination(
    int Page,
    int PageSize,
    long TotalRecords,
    int TotalPages);

public record PaginatedResult<T>(
    Pagination Pagination,
    IEnumerable<T> Data);
