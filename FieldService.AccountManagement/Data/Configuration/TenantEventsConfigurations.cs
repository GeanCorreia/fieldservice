using FieldService.AccountManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.AccountManagement.Data.Configuration;

internal sealed class TenantCreatedEventConfiguration : IEntityTypeConfiguration<TenantCreatedEvent>
{
    public void Configure(EntityTypeBuilder<TenantCreatedEvent> entity)
    {
        var eventType = (int)TenantEventType.TenantCreated;

        entity.Property(e => e.Name)
            .HasColumnName(TenantEventColumns.Name)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();

        entity.HasIndex(e => e.TenantId)
            .IsUnique()
            .HasDatabaseName("UX_TenantEvents_Tenant_Registered")
            .HasFilter($"\"EventType\" = {eventType}");
    }
}

internal sealed class TenantNameUpdatedEventConfiguration : IEntityTypeConfiguration<TenantNameUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<TenantNameUpdatedEvent> entity)
    {
        entity.Property(e => e.Name)
            .HasColumnName(TenantEventColumns.Name)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();
    }
}

