using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RevertMultiCharacterColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE job ADD COLUMN IF NOT EXISTS priority integer NOT NULL DEFAULT 2;");

            migrationBuilder.Sql(
                "ALTER TABLE profile ADD COLUMN IF NOT EXISTS pref_unavailable integer NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(
                "ALTER TABLE preference ADD COLUMN IF NOT EXISTS selected_character_slot integer NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(
                """
                UPDATE preference
                SET selected_character_slot =
                    (SELECT min(slot)
                     FROM profile
                     WHERE profile.preference_id = preference.preference_id
                     GROUP BY profile.preference_id)
                WHERE EXISTS (
                    SELECT 1 FROM profile WHERE profile.preference_id = preference.preference_id
                )
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE job DROP COLUMN IF EXISTS priority;");
            migrationBuilder.Sql("ALTER TABLE profile DROP COLUMN IF EXISTS pref_unavailable;");
            migrationBuilder.Sql("ALTER TABLE preference DROP COLUMN IF EXISTS selected_character_slot;");
        }
    }
}
