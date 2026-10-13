using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlertReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alertReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alertReceipts_systemUser_UserId",
                        column: x => x.UserId,
                        principalTable: "systemUser",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_alertReceipts_UserId_AlertKey",
                table: "alertReceipts",
                columns: new[] { "UserId", "AlertKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertReceipts");
        }
    }
}
