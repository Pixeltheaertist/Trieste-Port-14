using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class RestorePreferenceUnavailable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE profile ADD COLUMN IF NOT EXISTS pref_unavailable integer NOT NULL DEFAULT 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE profile DROP COLUMN IF EXISTS pref_unavailable;");
        }
    }
}
