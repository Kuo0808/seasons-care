using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    public partial class AlignHealthRecordCreatedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE blood_pressures ALTER COLUMN created_by TYPE text USING created_by::text;");
            migrationBuilder.Sql("ALTER TABLE blood_sugars ALTER COLUMN created_by TYPE text USING created_by::text;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE blood_pressures ALTER COLUMN created_by TYPE uuid USING created_by::uuid;");
            migrationBuilder.Sql("ALTER TABLE blood_sugars ALTER COLUMN created_by TYPE uuid USING created_by::uuid;");
        }
    }
}
