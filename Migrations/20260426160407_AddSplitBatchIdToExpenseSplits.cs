using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitBatchIdToExpenseSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "split_batch_id",
                table: "expense_splits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expense_splits_care_group_id_split_batch_id",
                table: "expense_splits",
                columns: new[] { "care_group_id", "split_batch_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_expense_splits_care_group_id_split_batch_id",
                table: "expense_splits");

            migrationBuilder.DropColumn(
                name: "split_batch_id",
                table: "expense_splits");
        }
    }
}
