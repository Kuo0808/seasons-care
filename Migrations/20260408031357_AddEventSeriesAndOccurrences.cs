using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeriesAndOccurrences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    repeat_pattern = table.Column<int>(type: "integer", nullable: false),
                    repeat_interval = table.Column<int>(type: "integer", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: true),
                    end_type = table.Column<int>(type: "integer", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    occurrence_count = table.Column<int>(type: "integer", nullable: true),
                    participants = table.Column<string[]>(type: "text[]", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_important = table.Column<bool>(type: "boolean", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_series", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_series_care_groups_care_group_id",
                        column: x => x.care_group_id,
                        principalTable: "care_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scheduled_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    override_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    override_description = table.Column<string>(type: "text", nullable: true),
                    override_participants = table.Column<string[]>(type: "text[]", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_important_override = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_occurrences_care_groups_care_group_id",
                        column: x => x.care_group_id,
                        principalTable: "care_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_occurrences_event_series_event_series_id",
                        column: x => x.event_series_id,
                        principalTable: "event_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_occurrences_care_group_id",
                table: "event_occurrences",
                column: "care_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_occurrences_event_series_id",
                table: "event_occurrences",
                column: "event_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_care_group_id",
                table: "event_series",
                column: "care_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_occurrences");

            migrationBuilder.DropTable(
                name: "event_series");
        }
    }
}
