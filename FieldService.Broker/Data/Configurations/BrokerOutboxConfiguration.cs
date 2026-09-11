using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Entities;
using FieldService.Broker.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Broker.Data.Configurations;

internal sealed class BrokerOutboxConfiguration : IEntityTypeConfiguration<BrokerOutbox>
{
    public void Configure(EntityTypeBuilder<BrokerOutbox> builder)
    {
        builder.ToTable("BrokerOutbox");

        builder.HasKey(x => x.MessageId);

        builder.Property(x => x.MessageId)
            .HasColumnName("MessageId")
            .ValueGeneratedNever();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(x => x.DispatchedAt)
            .HasColumnName("DispatchedAt");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("ExpiresAt");

        builder.Property("_message")
            .HasColumnName("Message")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Ignore(x => x.ServiceBusMessage);

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_BrokerOutbox_CreatedAt");

        builder.HasIndex(x => x.DispatchedAt)
            .HasDatabaseName("idx_BrokerOutbox_DispatchedAt");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_BrokerOutbox_ExpiresAt");
    }
}
