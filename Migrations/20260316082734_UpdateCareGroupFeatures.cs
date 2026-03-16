using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCareGroupFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "CareGroups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "CareGroups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "CareGroups",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "CareGroups");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "CareGroups");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "CareGroups");
        }
    }
}
