using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenBellScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                schema: "wordy",
                table: "GoldenBellSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScoringVersion",
                schema: "wordy",
                table: "GoldenBellSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimedOutCount",
                schema: "wordy",
                table: "GoldenBellSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "AnswerMs",
                schema: "wordy",
                table: "GoldenBellAttempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                schema: "wordy",
                table: "GoldenBellAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TimedOut",
                schema: "wordy",
                table: "GoldenBellAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                schema: "wordy",
                table: "GoldenBellSessions");

            migrationBuilder.DropColumn(
                name: "ScoringVersion",
                schema: "wordy",
                table: "GoldenBellSessions");

            migrationBuilder.DropColumn(
                name: "TimedOutCount",
                schema: "wordy",
                table: "GoldenBellSessions");

            migrationBuilder.DropColumn(
                name: "AnswerMs",
                schema: "wordy",
                table: "GoldenBellAttempts");

            migrationBuilder.DropColumn(
                name: "Points",
                schema: "wordy",
                table: "GoldenBellAttempts");

            migrationBuilder.DropColumn(
                name: "TimedOut",
                schema: "wordy",
                table: "GoldenBellAttempts");
        }
    }
}
