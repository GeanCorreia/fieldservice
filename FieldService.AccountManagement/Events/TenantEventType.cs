namespace FieldService.AccountManagement.Events;

public enum TenantEventType
{
    TenantCreated = 1,
    TenantNameUpdated = 2,
    TeamNameUpdated = 3,
    TeamDescriptionUpdated = 4,
    TeamLeaderUpdated = 5,
    JobTitleRegistered = 6,
    JobTitleDescriptionUpdated = 7,
    JobTitleTitleUpdated = 8,
    PersonalInfoFullNameUpdated = 9,
    PersonalInfoDisplayNameUpdated = 10,
    PersonalInfoEmailUpdated = 11,
    PersonalInfoPhoneUpdated = 12,
    UserAccountRegistered = 13,
    TeamRegistered = 14,
    PersonalInfoRegistered = 15,
    UserTeamTransferred = 16,
    TeamDeactivated = 17,
    UserJobTitleChanged = 18,
    UserAccountDeactivated = 19,
    UserAccountReactivated = 20,
    JobTitleDeactivated = 21
}
