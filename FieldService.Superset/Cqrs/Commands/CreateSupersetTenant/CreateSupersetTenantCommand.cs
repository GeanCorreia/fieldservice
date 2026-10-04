using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.CreateSupersetTenant;

public record CreateSupersetTenantCommand(
    Guid TenantId,
    SupersetTenantCreateParams CreateParams, 
    Guid? UserId = null) : IRequest;
