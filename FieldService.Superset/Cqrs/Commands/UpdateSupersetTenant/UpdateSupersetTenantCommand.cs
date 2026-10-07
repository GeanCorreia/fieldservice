using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.UpdateSupersetTenant;

public record UpdateSupersetTenantCommand(
    Guid TenantId,
    SupersetContainerConfiguration Configuration) : IRequest;