using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("OrganizationUnits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.OrganizationUnits)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentOrganizationUnit)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentOrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}