namespace FieldService.Shared.Responses;

public record PaginationResponse(
    int Page,
    int PageSize,
    long TotalRecords,
    int TotalPages);
