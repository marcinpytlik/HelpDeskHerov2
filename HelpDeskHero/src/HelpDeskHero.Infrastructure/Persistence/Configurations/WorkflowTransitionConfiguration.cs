using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.Transitions)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FromState)
            .WithMany(x => x.OutgoingTransitions)
            .HasForeignKey(x => x.FromStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToState)
            .WithMany(x => x.IncomingTransitions)
            .HasForeignKey(x => x.ToStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.FromStateId, x.ToStateId })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}