using FieldService.AccountManagement.Entities;

namespace FieldService.AccountManagement.Dtos;

internal record PersonalInfoDto
{
    public string FullName { get; }
    public string DisplayName { get; }
    public string Email { get; }
    public string Phone { get; }

    public PersonalInfoDto(
        string fullName,
        string displayName,
        string email,
        string phone)
    {
        FullName = fullName;
        DisplayName = displayName;
        Email = email;
        Phone = phone;
    }
}

internal static class PersonalInfoDtoExtensions
{
    public static PersonalInfoDto ToDto(this PersonalInfo personalInfo)
    {
        return new PersonalInfoDto(
            personalInfo.FullName,
            personalInfo.DisplayName,
            personalInfo.Email.Value,
            personalInfo.Phone.ToDigitsOnly());
    }
}

