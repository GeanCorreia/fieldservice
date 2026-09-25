using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public sealed record DownloadResponseDto(
    Guid FileId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    string HashMd5,
    string ContentBase64
) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(DownloadResponseDto);
    public override Guid? ResourceId => FileId;
}

