using HelpDeskHero.Domain.Common;
using HelpDeskHero.Domain.Enums;

namespace HelpDeskHero.Domain.Entities;

public sealed class OrganizationUnit : AuditableEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public int? ParentOrganizationUnitId { get; set; }
    public OrganizationUnit? ParentOrganizationUnit { get; set; }

    public ICollection<OrganizationUnit> Children { get; set; } = [];

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OrganizationUnitType Type { get; set; } = OrganizationUnitType.Department;

    public bool IsActive { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = [];
}