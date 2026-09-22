using FieldService.SignalR.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.SignalR.Data.Configuration;

internal sealed class DomainRoomEventConfiguration : IEntityTypeConfiguration<DomainRoomEvent>
{
    public void Configure(EntityTypeBuilder<DomainRoomEvent> builder)
    {
        builder.ToTable("DomainRoomEvent");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.RoomId)
            .HasColumnName("RoomId")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("EventType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("UserId");

        builder.Property(x => x.RoomName)
            .HasColumnName("RoomName")
            .HasMaxLength(256);

        builder.Property(x => x.RoomDescription)
            .HasColumnName("RoomDescription")
            .HasColumnType("jsonb");
        
        builder.HasIndex(x => x.RoomId)
            .HasDatabaseName("idx_DomainRoomEvent_RoomId");

        builder.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("idx_DomainRoomEvent_OccurredAt");

        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("idx_DomainRoomEvent_EventType");
    }
}

