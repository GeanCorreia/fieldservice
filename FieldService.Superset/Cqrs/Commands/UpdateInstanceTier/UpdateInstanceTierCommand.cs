using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.UpdateInstanceTier;

public record UpdateInstanceTierCommand(
    Guid TenantId,
    SupersetTenantCreateParams CreateParams) : IRequest;