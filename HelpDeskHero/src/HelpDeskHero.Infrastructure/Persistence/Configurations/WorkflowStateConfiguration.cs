using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowState>
{
    public void Configure(EntityTypeBuilder<WorkflowState> builder)
    {
        builder.ToTable("WorkflowStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.States)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.Code })
            .IsUnique();

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.SortOrder });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}