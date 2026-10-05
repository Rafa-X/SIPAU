using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScholarshipsAndTitulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_titulationProcesses_StudentId",
                table: "titulationProcesses");

            migrationBuilder.DropIndex(
                name: "IX_scolarshipAssigneds_StudentId",
                table: "scolarshipAssigneds");

            migrationBuilder.AlterColumn<string>(
                name: "ProfessionalLicenceNumber",
                table: "titulationProcesses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Modality",
                table: "titulationProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "scolarshipAssigneds",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateIndex(
                name: "IX_titulationProcesses_StudentId",
                table: "titulationProcesses",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scolarshipAssigneds_StudentId_PeriodId_ScolarshipProgramId",
                table: "scolarshipAssigneds",
                columns: new[] { "StudentId", "PeriodId", "ScolarshipProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_titulationProcesses_StudentId",
                table: "titulationProcesses");

            migrationBuilder.DropIndex(
                name: "IX_scolarshipAssigneds_StudentId_PeriodId_ScolarshipProgramId",
                table: "scolarshipAssigneds");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "titulationProcesses");

            migrationBuilder.AlterColumn<int>(
                name: "ProfessionalLicenceNumber",
                table: "titulationProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Amount",
                table: "scolarshipAssigneds",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_titulationProcesses_StudentId",
                table: "titulationProcesses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_scolarshipAssigneds_StudentId",
                table: "scolarshipAssigneds",
                column: "StudentId");
        }
    }
}
