using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskHero.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketListIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Tickets_TenantId_IsDeleted_CreatedAtUtc",
                table: "Tickets",
                newName: "IX_Tickets_TenantId_IsDeleted_CreatedAtUtc1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Tickets_TenantId_IsDeleted_CreatedAtUtc1",
                table: "Tickets",
                newName: "IX_Tickets_TenantId_IsDeleted_CreatedAtUtc");
        }
    }
}
