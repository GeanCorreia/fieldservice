using FieldService.AccountManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.AccountManagement.Data.Configuration;

internal sealed class UserAccountRegisteredEventConfiguration : IEntityTypeConfiguration<UserAccountRegisteredEvent>
{
    public void Configure(EntityTypeBuilder<UserAccountRegisteredEvent> entity)
    {
        var eventType = (int)TenantEventType.UserAccountRegistered;

        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();
        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();

        // UserId é uma coluna compartilhada com todos os eventos PersonalInfo*.
        entity.HasIndex(e => new { e.TenantId, e.UserId, e.EventType })
            .HasDatabaseName("IX_TenantEvents_Tenant_User_Type");

        entity.HasIndex(e => new { e.TenantId, e.UserId })
            .IsUnique()
            .HasDatabaseName("UX_TenantEvents_Tenant_User_Registered")
            .HasFilter($"\"EventType\" = {eventType}");
    }
}

internal sealed class PersonalInfoRegisteredEventConfiguration : IEntityTypeConfiguration<PersonalInfoRegisteredEvent>
{
    public void Configure(EntityTypeBuilder<PersonalInfoRegisteredEvent> entity)
    {
        var eventType = (int)TenantEventType.PersonalInfoRegistered;

        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();

        entity.Property(e => e.FullName)
            .HasColumnName(TenantEventColumns.FullName)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();

        entity.Property(e => e.DisplayName)
            .HasColumnName(TenantEventColumns.DisplayName)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();

        entity.Property(e => e.Email)
            .HasColumnName(TenantEventColumns.Email)
            .HasMaxLength(TenantEventColumns.EmailMaxLength)
            .HasConversion(TenantEventConverters.Email)
            .IsRequired();

        entity.Property(e => e.Phone)
            .HasColumnName(TenantEventColumns.Phone)
            .HasMaxLength(TenantEventColumns.PhoneMaxLength)
            .HasConversion(TenantEventConverters.Phone)
            .IsRequired();

        entity.HasIndex(e => new { e.TenantId, e.UserId })
            .IsUnique()
            .HasDatabaseName("UX_TenantEvents_Tenant_User_PersonalInfoRegistered")
            .HasFilter($"\"EventType\" = {eventType}");
    }
}

internal sealed class PersonalInfoFullNameUpdatedEventConfiguration : IEntityTypeConfiguration<PersonalInfoFullNameUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<PersonalInfoFullNameUpdatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();

        entity.Property(e => e.FullName)
            .HasColumnName(TenantEventColumns.FullName)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();
    }
}

internal sealed class PersonalInfoDisplayNameUpdatedEventConfiguration : IEntityTypeConfiguration<PersonalInfoDisplayNameUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<PersonalInfoDisplayNameUpdatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();

        entity.Property(e => e.DisplayName)
            .HasColumnName(TenantEventColumns.DisplayName)
            .HasMaxLength(TenantEventColumns.NameMaxLength)
            .IsRequired();
    }
}

internal sealed class PersonalInfoEmailUpdatedEventConfiguration : IEntityTypeConfiguration<PersonalInfoEmailUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<PersonalInfoEmailUpdatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();

        entity.Property(e => e.Email)
            .HasColumnName(TenantEventColumns.Email)
            .HasMaxLength(TenantEventColumns.EmailMaxLength)
            .HasConversion(TenantEventConverters.Email)
            .IsRequired();
    }
}

internal sealed class PersonalInfoPhoneUpdatedEventConfiguration : IEntityTypeConfiguration<PersonalInfoPhoneUpdatedEvent>
{
    public void Configure(EntityTypeBuilder<PersonalInfoPhoneUpdatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();

        entity.Property(e => e.Phone)
            .HasColumnName(TenantEventColumns.Phone)
            .HasMaxLength(TenantEventColumns.PhoneMaxLength)
            .HasConversion(TenantEventConverters.Phone)
            .IsRequired();
    }
}

internal sealed class UserJobTitleChangedEventConfiguration : IEntityTypeConfiguration<UserJobTitleChangedEvent>
{
    public void Configure(EntityTypeBuilder<UserJobTitleChangedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();
        entity.Property(e => e.JobTitleId).HasColumnName(TenantEventColumns.JobTitleId).IsRequired();
    }
}

internal sealed class UserAccountDeactivatedEventConfiguration : IEntityTypeConfiguration<UserAccountDeactivatedEvent>
{
    public void Configure(EntityTypeBuilder<UserAccountDeactivatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();
    }
}

internal sealed class UserAccountReactivatedEventConfiguration : IEntityTypeConfiguration<UserAccountReactivatedEvent>
{
    public void Configure(EntityTypeBuilder<UserAccountReactivatedEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();
    }
}

internal sealed class UserTeamTransferredEventConfiguration : IEntityTypeConfiguration<UserTeamTransferredEvent>
{
    public void Configure(EntityTypeBuilder<UserTeamTransferredEvent> entity)
    {
        entity.Property(e => e.UserId).HasColumnName(TenantEventColumns.UserId).IsRequired();
        entity.Property(e => e.TeamId).HasColumnName(TenantEventColumns.TeamId).IsRequired();

        // Supports historical queries of team transfers by tenant/user in chronological order.
        entity.HasIndex(e => new { e.TenantId, e.UserId, e.CreatedAt })
            .HasDatabaseName("IX_TenantEvents_Tenant_User_TeamTransfer_CreatedAt")
            .HasFilter($"\"EventType\" = {(int)TenantEventType.UserTeamTransferred}");
    }
}
