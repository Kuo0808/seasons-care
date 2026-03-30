using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLastViewedCareGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "last_viewed_care_group_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_last_viewed_care_group_id",
                table: "users",
                column: "last_viewed_care_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_care_groups_last_viewed_care_group_id",
                table: "users",
                column: "last_viewed_care_group_id",
                principalTable: "care_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_care_groups_last_viewed_care_group_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_last_viewed_care_group_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_viewed_care_group_id",
                table: "users");
        }
    }
}
