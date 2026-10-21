using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentCareerAndEntryPeriodKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_students_CareerId",
                table: "students",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_students_PeriodIdStart",
                table: "students",
                column: "PeriodIdStart");

            migrationBuilder.AddForeignKey(
                name: "FK_students_careers_CareerId",
                table: "students",
                column: "CareerId",
                principalTable: "careers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_students_periods_PeriodIdStart",
                table: "students",
                column: "PeriodIdStart",
                principalTable: "periods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_students_careers_CareerId",
                table: "students");

            migrationBuilder.DropForeignKey(
                name: "FK_students_periods_PeriodIdStart",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_CareerId",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_PeriodIdStart",
                table: "students");
        }
    }
}
