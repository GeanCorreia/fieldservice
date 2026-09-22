using System.Text.Json;
using FieldService.SignalR.Entities;
using FieldService.SignalR.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.SignalR.Data.Configuration;

internal sealed class DomainRoomConfiguration : IEntityTypeConfiguration<DomainRoom>
{
    public void Configure(EntityTypeBuilder<DomainRoom> builder)
    {
        builder.ToTable("DomainRoom");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        builder.Ignore(x => x.UserMemberIds);

        builder.HasMany<DomainRoomEvent>("_events")
            .WithOne()
            .HasForeignKey(x => x.RoomId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("idx_DomainRoom_TenantId");
    }
}

