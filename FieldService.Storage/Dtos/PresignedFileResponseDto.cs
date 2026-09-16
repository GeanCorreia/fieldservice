using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Storage.Dtos;

public record PresignedFileUploadResponseDto(
    Guid FileId,
    string UploadUrl,
    DateTimeOffset ExpiresAt
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedFileUploadResponseDto";
    public override bool IsActive => true;
}

public record PresignedBatchFileUploadResponseDto(
    IReadOnlyCollection<PresignedFileUploadResponseDto> Uploads
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedBatchFileUploadResponseDto";
    public override bool IsActive => true;
}

public record PresignedFileDownloadResponseDto(
    Guid FileId,
    string FileName,
    long SizeInBytes,
    string HashMd5,
    string ContentType,
    string DownloadUrl,
    DateTimeOffset ExpiresAt
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedFileDownloadResponseDto";
    public override bool IsActive => true;
}

public record PresignedBatchFileDownloadResponseDto(
    IReadOnlyCollection<PresignedFileDownloadResponseDto> Downloads
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "PresignedBatchFileDownloadResponseDto";
    public override bool IsActive => true;
}