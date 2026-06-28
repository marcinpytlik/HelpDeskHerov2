using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.IsInternal)
            .IsRequired();

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAtUtc });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}