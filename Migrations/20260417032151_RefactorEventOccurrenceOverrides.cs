using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEventOccurrenceOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "scheduled_start_at",
                table: "event_occurrences",
                newName: "occurrence_key_start_at");

            migrationBuilder.RenameColumn(
                name: "scheduled_end_at",
                table: "event_occurrences",
                newName: "override_end_at");

            migrationBuilder.RenameIndex(
                name: "ix_event_occurrences_event_series_id_scheduled_start_at",
                table: "event_occurrences",
                newName: "ix_event_occurrences_event_series_id_occurrence_key_start_at");

            migrationBuilder.AlterColumn<bool>(
                name: "is_important_override",
                table: "event_occurrences",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.Sql("""
                UPDATE event_occurrences
                SET is_important_override = NULL
                WHERE is_important_override = FALSE;
                """);

            migrationBuilder.RenameColumn(
                name: "is_important_override",
                table: "event_occurrences",
                newName: "override_is_important");

            migrationBuilder.AddColumn<DateTime>(
                name: "override_start_at",
                table: "event_occurrences",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "override_start_at",
                table: "event_occurrences");

            migrationBuilder.RenameColumn(
                name: "override_is_important",
                table: "event_occurrences",
                newName: "is_important_override");

            migrationBuilder.RenameColumn(
                name: "occurrence_key_start_at",
                table: "event_occurrences",
                newName: "scheduled_start_at");

            migrationBuilder.RenameColumn(
                name: "override_end_at",
                table: "event_occurrences",
                newName: "scheduled_end_at");

            migrationBuilder.RenameIndex(
                name: "ix_event_occurrences_event_series_id_occurrence_key_start_at",
                table: "event_occurrences",
                newName: "ix_event_occurrences_event_series_id_scheduled_start_at");

            migrationBuilder.Sql("""
                UPDATE event_occurrences
                SET is_important_override = FALSE
                WHERE is_important_override IS NULL;
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "is_important_override",
                table: "event_occurrences",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }
    }
}
