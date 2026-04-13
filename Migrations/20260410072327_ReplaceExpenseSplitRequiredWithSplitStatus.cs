using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceExpenseSplitRequiredWithSplitStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "split_status",
                table: "expenses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'expenses' AND column_name = 'is_split_required'
                    ) THEN
                        UPDATE expenses
                        SET split_status = CASE
                            WHEN is_split_required = TRUE THEN 'Pending'
                            ELSE 'None'
                        END;
                    ELSE
                        UPDATE expenses
                        SET split_status = COALESCE(split_status, 'None');
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql("""
                UPDATE expenses
                SET category = 'other'
                WHERE category IS NULL OR btrim(category) = ''
                """);

            migrationBuilder.Sql("""
                UPDATE expenses
                SET category = CASE
                    WHEN lower(btrim(category)) = 'medical' THEN 'medical'
                    WHEN lower(btrim(category)) = 'food' THEN 'food'
                    WHEN lower(btrim(category)) = 'traffic' THEN 'traffic'
                    WHEN lower(btrim(category)) = 'other' THEN 'other'
                    WHEN lower(btrim(category)) = 'daily' THEN 'food'
                    WHEN lower(btrim(category)) = 'transport' THEN 'traffic'
                    ELSE 'other'
                END
                """);

            migrationBuilder.Sql("""
                UPDATE expenses
                SET notes = left(notes, 500)
                WHERE notes IS NOT NULL AND length(notes) > 500
                """);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "expenses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "expenses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "split_status",
                table: "expenses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE expenses
                DROP COLUMN IF EXISTS is_split_required;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_split_required",
                table: "expenses",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE expenses
                SET is_split_required = CASE
                    WHEN split_status IN ('Pending', 'Settled') THEN TRUE
                    ELSE FALSE
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "expenses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "expenses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "is_split_required",
                table: "expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "split_status",
                table: "expenses");
        }
    }
}
