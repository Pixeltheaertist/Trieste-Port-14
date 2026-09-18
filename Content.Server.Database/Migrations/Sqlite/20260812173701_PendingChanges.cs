using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite has no IF NOT EXISTS for ADD COLUMN - left as a normal EF operation.
            // Not a production risk: this only runs against dev's local .sqlite file,
            // which gets recreated fresh rather than accumulating stray tables.
            migrationBuilder.AddColumn<string>(
                name: "custom_species_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS star_light_profile (
                    star_light_profile_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    profile_id INTEGER NOT NULL,
                    custom_species_name TEXT,
                    CONSTRAINT ""FK_star_light_profile_profile_profile_id""
                        FOREIGN KEY (profile_id)
                        REFERENCES profile (profile_id)
                        ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_star_light_profile_profile_id\" ON star_light_profile(profile_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS star_light_profile;");

            migrationBuilder.DropColumn(
                name: "custom_species_name",
                table: "profile");
        }
    }
}
