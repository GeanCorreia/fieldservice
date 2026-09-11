namespace FieldService.Shared.Responses;

public record ApiResponse(
    Guid RequestId,
    DateTimeOffset OccurredAt,
    object? Data,
    PaginationResponse? Pagination);
