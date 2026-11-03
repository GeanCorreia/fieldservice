using FieldService.AccountManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.AccountManagement.Data.Configuration;

internal sealed class JobTitleRegisteredEventConfiguration : IEntityTypeConfiguration<JobTitleRegisteredEvent>
{
    public void Configure(EntityTypeBuilder<JobTitleRegisteredEvent> entity)
    {
        var eventType = (int)TenantEventType.JobTitleRegistered;

        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();

        entity.Property(e => e.Title)
            .HasColumnName(TenantEventColumns.Title)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();

        // JobTitleId é uma coluna compartilhada (também usada por UserAccountRegistered).
        entity.HasIndex(e => new { e.TenantId, e.JobTitleId, e.EventType })
            .HasDatabaseName("IX_TenantEvents_Tenant_JobTitle_Type");

        entity.HasIndex(e => new { e.TenantId, e.JobTitleId })
            .IsUnique()
            .HasDatabaseName("UX_TenantEvents_Tenant_JobTitle_Registered")
            .HasFilter($"\"EventType\" = {eventType}");
    }
}

internal sealed class JobTitleTitleUpdatedEventConfiguration : IEntityTypeConfiguration<JobTitleTitleUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<JobTitleTitleUpdatedEvent> entity)
    {
        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();

        entity.Property(e => e.Title)
            .HasColumnName(TenantEventColumns.Title)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();
    }
}

internal sealed class JobTitleDescriptionUpdatedEventConfiguration : IEntityTypeConfiguration<JobTitleDescriptionUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<JobTitleDescriptionUpdatedEvent> entity)
    {
        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();

        entity.Property(e => e.Description)
            .HasColumnName(TenantEventColumns.Description)
            .HasMaxLength(TenantEventColumns.DescriptionMaxLength);
    }
}

internal sealed class JobTitleDeactivatedEventConfiguration : IEntityTypeConfiguration<JobTitleDeactivatedEvent>
{
    public void Configure(EntityTypeBuilder<JobTitleDeactivatedEvent> entity)
    {
        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();
    }
}
