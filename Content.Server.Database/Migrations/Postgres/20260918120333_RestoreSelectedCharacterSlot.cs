using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RestoreSelectedCharacterSlot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE preference ADD COLUMN IF NOT EXISTS selected_character_slot integer NOT NULL DEFAULT 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE preference DROP COLUMN IF EXISTS selected_character_slot;");
        }
    }
}
