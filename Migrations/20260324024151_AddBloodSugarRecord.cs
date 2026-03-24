using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodSugarRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_sugars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    glucose_level = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    measurement_context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    record_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blood_sugars", x => x.id);
                    table.ForeignKey(
                        name: "fk_blood_sugars_care_groups_care_group_id",
                        column: x => x.care_group_id,
                        principalTable: "care_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_blood_sugars_care_group_id",
                table: "blood_sugars",
                column: "care_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_sugars");
        }
    }
}
