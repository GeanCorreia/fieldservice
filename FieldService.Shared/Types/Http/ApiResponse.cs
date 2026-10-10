namespace FieldService.Shared.Types.Http;

public record ApiResponse(
    Guid RequestId,
    DateTimeOffset OccurredAt,
    object? Data,
    Pagination? Pagination);
