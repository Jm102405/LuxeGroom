using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxeGroom.Migrations
{
    /// <inheritdoc />
    public partial class SyncExistingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tables already exist in SSMS — intentionally left empty
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty
        }
    }
}
