using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventOccurrenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_occurrences_event_series_id",
                table: "event_occurrences");

            migrationBuilder.CreateIndex(
                name: "ix_event_occurrences_event_series_id_scheduled_start_at",
                table: "event_occurrences",
                columns: new[] { "event_series_id", "scheduled_start_at" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_occurrences_event_series_id_scheduled_start_at",
                table: "event_occurrences");

            migrationBuilder.CreateIndex(
                name: "ix_event_occurrences_event_series_id",
                table: "event_occurrences",
                column: "event_series_id");
        }
    }
}
