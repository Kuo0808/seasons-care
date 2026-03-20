using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCareLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "care_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    log_type = table.Column<string>(type: "text", nullable: true),
                    record_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_care_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_care_logs_care_groups_care_group_id",
                        column: x => x.care_group_id,
                        principalTable: "care_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_care_logs_care_group_id",
                table: "care_logs",
                column: "care_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "care_logs");
        }
    }
}
