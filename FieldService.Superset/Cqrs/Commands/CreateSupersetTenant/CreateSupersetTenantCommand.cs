using FieldService.Superset.Dtos;
using FieldService.Cloud.Entities;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.CreateSupersetTenant;

public record CreateSupersetTenantCommand(
    Guid TenantId,
    ContainerConfiguration Configuration, 
    Guid? DedicatedHostConnectionStringId = null) : IRequest;
