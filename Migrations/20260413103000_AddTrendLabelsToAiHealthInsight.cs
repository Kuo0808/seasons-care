using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SeasonsCare.Api.Data;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260413103000_AddTrendLabelsToAiHealthInsight")]
    public partial class AddTrendLabelsToAiHealthInsight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE ai_health_insights
                ADD COLUMN IF NOT EXISTS trend_labels text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE ai_health_insights
                DROP COLUMN IF EXISTS trend_labels;
                """);
        }
    }
}
