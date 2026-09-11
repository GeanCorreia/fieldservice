using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public record PaginationRequestDto(
    int Page = 1,
    int PageSize = 20,
    bool IsDescending = false);
