using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TicketHistoryEntryConfiguration : IEntityTypeConfiguration<TicketHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TicketHistoryEntry> builder)
    {
        builder.ToTable("TicketHistoryEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OldValue)
            .HasMaxLength(500);

        builder.Property(x => x.NewValue)
            .HasMaxLength(500);

        builder.Property(x => x.Comment)
            .HasMaxLength(4000);

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAtUtc });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}