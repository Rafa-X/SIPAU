using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FrozenBoardReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_BoardNarrative",
                table: "reports",
                column: "TemplateVersion",
                unique: true,
                filter: "[ReportType] = N'InformeJunta' AND [TemplateVersion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_periods_ActivePeriod",
                table: "periods",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_periods_IdCode",
                table: "periods",
                column: "IdCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reports_BoardNarrative",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "IX_periods_ActivePeriod",
                table: "periods");

            migrationBuilder.DropIndex(
                name: "IX_periods_IdCode",
                table: "periods");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "reports");
        }
    }
}
