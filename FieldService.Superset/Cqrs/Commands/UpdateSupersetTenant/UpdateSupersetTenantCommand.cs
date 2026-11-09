using FieldService.Superset.Dtos;
using FieldService.Cloud.Entities;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.UpdateSupersetTenant;

public record UpdateSupersetTenantCommand(
    Guid TenantId,
    ContainerConfiguration Configuration) : IRequest;