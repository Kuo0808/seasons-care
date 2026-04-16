using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_splits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_payer = table.Column<bool>(type: "boolean", nullable: false),
                    care_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_splits", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_splits_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expense_splits_care_group_id_user_id",
                table: "expense_splits",
                columns: new[] { "care_group_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expense_splits_expense_id_user_id",
                table: "expense_splits",
                columns: new[] { "expense_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_splits");
        }
    }
}
