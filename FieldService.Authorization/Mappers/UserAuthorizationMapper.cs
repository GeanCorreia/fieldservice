using FieldService.Authorization.Entities;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Types;
using FieldService.Shared.Interfaces;

namespace FieldService.Authorization.Mappers;

public class UserAuthorizationMapper : IUserAuthorizationMapper
{
    private readonly IDateTimeService _dateTimeService;

    public UserAuthorizationMapper(IDateTimeService dateTimeService)
    {
        _dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
    }

    public UserAuthorizationSnapshot Map(UserAuthorizationContext user)
    {
        var now = _dateTimeService.Now();
        return new UserAuthorizationSnapshot(
            user.UserId,
            user.TenantId,
            user.Role,
            user.Permissions.ToList(),
            user.IsActive(now),
            now
        );
    }
}