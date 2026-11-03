using FieldService.AccountManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.AccountManagement.Data.Configuration;

internal sealed class TeamRegisteredEventConfiguration : IEntityTypeConfiguration<TeamRegisteredEvent>
{
    public void Configure(EntityTypeBuilder<TeamRegisteredEvent> entity)
    {
        var eventType = (int)TenantEventType.TeamRegistered;

        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();

        entity.Property(e => e.Name)
            .HasColumnName(TenantEventColumns.Name)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();

        entity.Property(e => e.Description)
            .HasColumnName(TenantEventColumns.Description)
            .HasMaxLength(TenantEventColumns.DescriptionMaxLength);

        entity.Property(e => e.LeaderId).HasColumnName(TenantEventColumns.LeaderId);

        // TeamId é uma coluna compartilhada (também usada por UserAccountRegistered),
        // portanto este índice atende buscas por time e por usuários do time.
        entity.HasIndex(e => new { e.TenantId, e.TeamId, e.EventType })
            .HasDatabaseName("IX_TenantEvents_Tenant_Team_Type");

        entity.HasIndex(e => new { e.TenantId, e.TeamId })
            .IsUnique()
            .HasDatabaseName("UX_TenantEvents_Tenant_Team_Registered")
            .HasFilter($"\"EventType\" = {eventType}");
    }
}

internal sealed class TeamNameUpdatedEventConfiguration : IEntityTypeConfiguration<TeamNameUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<TeamNameUpdatedEvent> entity)
    {
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();

        entity.Property(e => e.Name)
            .HasColumnName(TenantEventColumns.Name)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();
    }
}

internal sealed class TeamDescriptionUpdatedEventConfiguration : IEntityTypeConfiguration<TeamDescriptionUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<TeamDescriptionUpdatedEvent> entity)
    {
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();

        entity.Property(e => e.Description)
            .HasColumnName(TenantEventColumns.Description)
            .HasMaxLength(TenantEventColumns.DescriptionMaxLength);
    }
}

internal sealed class TeamLeaderUpdatedEventConfiguration : IEntityTypeConfiguration<TeamLeaderUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<TeamLeaderUpdatedEvent> entity)
    {
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();
        entity.Property(e => e.LeaderId).HasColumnName(TenantEventColumns.LeaderId);
    }
}

internal sealed class TeamDeactivatedEventConfiguration : IEntityTypeConfiguration<TeamDeactivatedEvent>
{
    public void Configure(EntityTypeBuilder<TeamDeactivatedEvent> entity)
    {
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();

        entity.HasIndex(e => new { e.TenantId, e.TeamId, e.CreatedAt })
            .HasDatabaseName("IX_TenantEvents_Tenant_Team_Deactivated_CreatedAt")
            .HasFilter($"\"EventType\" = {(int)TenantEventType.TeamDeactivated}");
    }
}

