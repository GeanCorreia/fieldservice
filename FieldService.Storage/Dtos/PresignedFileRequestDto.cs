using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Storage.Dtos;

public record PresignedFileUploadRequestDto(
    string FileName,
    long SizeInBytes,
    string HashMd5,
    Guid CategoryId
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedFileUploadRequestDto";
    public override bool IsActive => true;
}



public record PresignedBatchFileUploadRequestDto(
    IReadOnlyCollection<PresignedFileUploadRequestDto> Files,
    bool PartialSuccess = false
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedBatchFileUploadRequestDto";
    public override bool IsActive => true;
}

public record PresignedFileDownloadRequestDto(
    Guid Id
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedFileDownloadRequestDto";
    public override bool IsActive => true;
}

public record PresignedBatchFileDownloadRequestDto(
    IReadOnlyCollection<Guid> FileIds,
    bool PartialSuccess = false
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedBatchFileDownloadRequestDto";
    public override bool IsActive => true;
}