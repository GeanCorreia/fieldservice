using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Events;

namespace FieldService.AccountManagement.Interfaces;

internal interface ITenantEntity
{
    public Guid TenantId { get; }

    public ICollection<TenantEventBase> Events { get; }
}