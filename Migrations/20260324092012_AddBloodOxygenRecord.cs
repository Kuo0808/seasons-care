using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodOxygenRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_oxygens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sp_o2 = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    record_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blood_oxygens", x => x.id);
                    table.ForeignKey(
                        name: "fk_blood_oxygens_care_groups_care_group_id",
                        column: x => x.care_group_id,
                        principalTable: "care_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_blood_oxygens_care_group_id",
                table: "blood_oxygens",
                column: "care_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_oxygens");
        }
    }
}
