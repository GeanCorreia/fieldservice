using System.Text.Json;
using FieldService.Form.Entities;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Form.Dtos;

public sealed record FormSubmissionDto(
    Guid SubmissionId,
    Guid FormId,
    JsonElement DataJson,
    DateTimeOffset LastModifiedAt,
    bool IsDeleted,
    Guid? DeletedByUserId) : AbstractDto
{
    public override bool IsActive => !IsDeleted;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(FormSubmissionDto);
    public override string? ResourceId => SubmissionId.ToString();
}

internal static class FormSubmissionDtoExtensions
{
    public static FormSubmissionDto ToDto(this FormSubmission submission)
    {
        return new FormSubmissionDto(
            SubmissionId: submission.Id,
            FormId: submission.FormId,
            DataJson: submission.DataJson,
            LastModifiedAt: submission.LastModifiedAt,
            IsDeleted: submission.IsDelete,
            DeletedByUserId: submission.DeleteByUserId);
    }
}

