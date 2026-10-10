using System.Text.Json;
using FiledService.Shared.Attributes;

namespace FieldService.Form.Entities;

[Auditable(resource: nameof(FormSubmission), resourceIdPropertyName: nameof(FormSubmission.Id))]
internal class FormSubmission
{
    public Guid Id { get; init; }
    public Guid FormId { get; init; }
    public JsonElement DataJson { get; private set; }
    public DateTimeOffset LastModifiedAt { get; private set; }
    public bool IsDelete { get; private set; }
    public Guid? DeleteByUserId { get; private set; }

    protected FormSubmission() { }

    private FormSubmission(
        Guid id,
        Guid formId,
        JsonElement dataJson)
    {
        Id = id;
        FormId = formId;
        DataJson = dataJson.Clone();
        LastModifiedAt = DateTimeOffset.UtcNow;
        IsDelete = false;
    }

    public static FormSubmission CreateRegister(
        Guid formId,
        JsonElement dataJson)
    {
        return new FormSubmission(
            id: Guid.NewGuid(),
            formId: formId,
            dataJson: dataJson);
    }

    public void UpdateData(JsonElement newDataJson)
    {
        if (IsDelete)
            throw new InvalidOperationException("Cannot update data of a deleted submission.");

        DataJson = newDataJson.Clone();
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDeleted(Guid userId)
    {
        if (IsDelete)
            return;

        IsDelete = true;
        DeleteByUserId = userId;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void UndoDeletion()
    {
        if (!IsDelete)
            return;

        IsDelete = false;
        DeleteByUserId = null;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }
}