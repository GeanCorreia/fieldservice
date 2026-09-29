using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Storage.Dtos;

public record PresignedFileUploadRequestDto(
    string FileName,
    long SizeInBytes,
    string HashMd5,
    Guid CategoryId
);



public record PresignedBatchFileUploadRequestDto(
    IReadOnlyCollection<PresignedFileUploadRequestDto> Files,
    bool PartialSuccess = false
);

public record PresignedFileDownloadRequestDto(
    Guid Id
);

public record PresignedBatchFileDownloadRequestDto(
    IReadOnlyCollection<Guid> FileIds,
    bool PartialSuccess = false
);