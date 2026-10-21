using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPZMG.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStudentIndigenousLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // test-data.sql stored each test student's language as a bare id instead of the JSON
            // list EF reads, so opening any of those students failed. Wrap each such id in a list;
            // values that are already JSON, or are not an id, stay as they are.
            migrationBuilder.Sql("""
                UPDATE students
                SET IndigenousLanguages = CONCAT('["', LOWER(IndigenousLanguages), '"]')
                WHERE ISJSON(IndigenousLanguages) = 0
                  AND TRY_CONVERT(uniqueidentifier, IndigenousLanguages) IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the application could not read the bare ids back.
        }
    }
}
