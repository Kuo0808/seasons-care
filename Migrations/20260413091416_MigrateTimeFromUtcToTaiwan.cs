using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigrateTimeFromUtcToTaiwan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                UPDATE ""Users"" SET ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""CareGroups"" SET ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""CareGroupMembers"" SET ""JoinedAt"" = ""JoinedAt"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""CareLogs"" SET ""StartsAt"" = ""StartsAt"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""ExpenseRecords"" SET ""ExpenseDate"" = ""ExpenseDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""EventSeries"" SET ""StartsAt"" = ""StartsAt"" + interval '8 hours', ""EndAt"" = ""EndAt"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""EventOccurrences"" SET ""ScheduledStartAt"" = ""ScheduledStartAt"" + interval '8 hours', ""ScheduledEndAt"" = ""ScheduledEndAt"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""AiHealthInsights"" SET ""GeneratedAt"" = ""GeneratedAt"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""BloodPressureRecords"" SET ""RecordDate"" = ""RecordDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""BloodSugarRecords"" SET ""RecordDate"" = ""RecordDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""WeightRecords"" SET ""RecordDate"" = ""RecordDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""TemperatureRecords"" SET ""RecordDate"" = ""RecordDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
                UPDATE ""BloodOxygenRecords"" SET ""RecordDate"" = ""RecordDate"" + interval '8 hours', ""CreatedAt"" = ""CreatedAt"" + interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" + interval '8 hours', ""DeletedAt"" = ""DeletedAt"" + interval '8 hours';
            ";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                UPDATE ""Users"" SET ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""CareGroups"" SET ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""CareGroupMembers"" SET ""JoinedAt"" = ""JoinedAt"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""CareLogs"" SET ""StartsAt"" = ""StartsAt"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""ExpenseRecords"" SET ""ExpenseDate"" = ""ExpenseDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""EventSeries"" SET ""StartsAt"" = ""StartsAt"" - interval '8 hours', ""EndAt"" = ""EndAt"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""EventOccurrences"" SET ""ScheduledStartAt"" = ""ScheduledStartAt"" - interval '8 hours', ""ScheduledEndAt"" = ""ScheduledEndAt"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""AiHealthInsights"" SET ""GeneratedAt"" = ""GeneratedAt"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""BloodPressureRecords"" SET ""RecordDate"" = ""RecordDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""BloodSugarRecords"" SET ""RecordDate"" = ""RecordDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""WeightRecords"" SET ""RecordDate"" = ""RecordDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""TemperatureRecords"" SET ""RecordDate"" = ""RecordDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
                UPDATE ""BloodOxygenRecords"" SET ""RecordDate"" = ""RecordDate"" - interval '8 hours', ""CreatedAt"" = ""CreatedAt"" - interval '8 hours', ""UpdatedAt"" = ""UpdatedAt"" - interval '8 hours', ""DeletedAt"" = ""DeletedAt"" - interval '8 hours';
            ";
            migrationBuilder.Sql(sql);
        }
    }
}
