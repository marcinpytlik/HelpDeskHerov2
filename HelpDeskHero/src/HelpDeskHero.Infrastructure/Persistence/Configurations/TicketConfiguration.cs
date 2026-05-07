using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.AssignedToUserId)
            .HasMaxLength(450);

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TicketType)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkflowState)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.WorkflowStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Number })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.WorkflowStateId, x.CreatedAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.AssignedToUserId, x.CreatedAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.IsDeleted, x.CreatedAtUtc });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}