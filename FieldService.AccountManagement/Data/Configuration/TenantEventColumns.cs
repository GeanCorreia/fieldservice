using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FieldService.AccountManagement.Data.Configuration;

/// <summary>
/// Nomes das colunas compartilhadas da tabela única TenantEvents (TPH).
/// Eventos diferentes que usam o mesmo nome apontam para a mesma coluna física.
/// </summary>
internal static class TenantEventColumns
{
    public const string TeamId = "TeamId";
    public const string PreviousTeamId = "PreviousTeamId";
    public const string UserId = "UserId";
    public const string JobTitleId = "JobTitleId";
    public const string LeaderId = "LeaderId";
    public const string Name = "Name";
    public const string Description = "Description";
    public const string Title = "Title";
    public const string FullName = "FullName";
    public const string DisplayName = "DisplayName";
    public const string Email = "Email";
    public const string Phone = "Phone";

    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 11;
}

internal static class TenantEventConverters
{
    public static readonly ValueConverter<Email, string> Email =
        new(v => v.Value, v => new Email(v));

    public static readonly ValueConverter<Phone, string> Phone =
        new(v => v.ToDigitsOnly(), v => new Phone(v));
}

