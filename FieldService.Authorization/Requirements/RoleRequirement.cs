using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Requirements;

public sealed record RoleRequirement(Role Role) : IAuthorizationRequirement;
