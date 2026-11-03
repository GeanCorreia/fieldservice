using FieldService.AccountManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.AccountManagement.Data.Configuration;

/// <summary>
/// Tabela única de eventos (TPH). Cada evento possui sua própria configuration
/// com as colunas obrigatórias específicas.
/// </summary>
internal sealed class TenantEventConfiguration : IEntityTypeConfiguration<TenantEventBase>
{
    public void Configure(EntityTypeBuilder<TenantEventBase> entity)
    {
        entity.ToTable("TenantEvents");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.TenantId).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.CreatedBy).IsRequired();

        entity.HasDiscriminator(e => e.EventType)
            .HasValue<TenantCreatedEvent>(TenantEventType.TenantCreated)
            .HasValue<TenantNameUpdatedEvent>(TenantEventType.TenantNameUpdated)
            .HasValue<TeamNameUpdatedEvent>(TenantEventType.TeamNameUpdated)
            .HasValue<TeamDescriptionUpdatedEvent>(TenantEventType.TeamDescriptionUpdated)
            .HasValue<TeamLeaderUpdatedEvent>(TenantEventType.TeamLeaderUpdated)
            .HasValue<JobTitleRegisteredEvent>(TenantEventType.JobTitleRegistered)
            .HasValue<JobTitleDescriptionUpdatedEvent>(TenantEventType.JobTitleDescriptionUpdated)
            .HasValue<JobTitleTitleUpdatedEvent>(TenantEventType.JobTitleTitleUpdated)
            .HasValue<PersonalInfoFullNameUpdatedEvent>(TenantEventType.PersonalInfoFullNameUpdated)
            .HasValue<PersonalInfoDisplayNameUpdatedEvent>(TenantEventType.PersonalInfoDisplayNameUpdated)
            .HasValue<PersonalInfoEmailUpdatedEvent>(TenantEventType.PersonalInfoEmailUpdated)
            .HasValue<PersonalInfoPhoneUpdatedEvent>(TenantEventType.PersonalInfoPhoneUpdated)
            .HasValue<UserAccountRegisteredEvent>(TenantEventType.UserAccountRegistered)
            .HasValue<TeamRegisteredEvent>(TenantEventType.TeamRegistered)
            .HasValue<PersonalInfoRegisteredEvent>(TenantEventType.PersonalInfoRegistered)
            .HasValue<UserTeamTransferredEvent>(TenantEventType.UserTeamTransferred)
            .HasValue<UserJobTitleChangedEvent>(TenantEventType.UserJobTitleChanged)
            .HasValue<TeamDeactivatedEvent>(TenantEventType.TeamDeactivated)
            .HasValue<UserAccountDeactivatedEvent>(TenantEventType.UserAccountDeactivated)
            .HasValue<UserAccountReactivatedEvent>(TenantEventType.UserAccountReactivated)
            .HasValue<JobTitleDeactivatedEvent>(TenantEventType.JobTitleDeactivated);

        // Tenant + tipo de evento (+ ordenação cronológica): base de todas as consultas.
        entity.HasIndex(e => new { e.TenantId, e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_TenantEvents_Tenant_Type_CreatedAt");
    }
}
