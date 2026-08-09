using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Requirements;

public sealed record PermissionRequirement(Permission Permission) : IAuthorizationRequirement;
