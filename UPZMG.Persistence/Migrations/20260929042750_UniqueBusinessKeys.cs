using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueBusinessKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_systemUser_Email",
                table: "systemUser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_CURP",
                table: "students",
                column: "CURP",
                unique: true,
                filter: "[CURP] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_students_EnrollmentNumber",
                table: "students",
                column: "EnrollmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Curp",
                table: "employees",
                column: "Curp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Email",
                table: "employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeNumber",
                table: "employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Rfc",
                table: "employees",
                column: "Rfc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_systemUser_Email",
                table: "systemUser");

            migrationBuilder.DropIndex(
                name: "IX_students_CURP",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_EnrollmentNumber",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_employees_Curp",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_Email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_EmployeeNumber",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_Rfc",
                table: "employees");
        }
    }
}
