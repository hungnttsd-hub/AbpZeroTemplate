using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenBell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldenBellQuestions",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldenBellQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldenBellSessions",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<long>(type: "bigint", nullable: false),
                    BankVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    QuestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CurrentQuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    WrongAttempts = table.Column<int>(type: "integer", nullable: false),
                    HintCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    BellRung = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldenBellSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldenBellSessions_Children_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalSchema: "wordy",
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoldenBellAttempts",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionCode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    QuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    HintUsed = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldenBellAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldenBellAttempts_GoldenBellSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "wordy",
                        principalTable: "GoldenBellSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoldenBellAttempts_SessionId_QuestionIndex_AttemptNumber",
                schema: "wordy",
                table: "GoldenBellAttempts",
                columns: new[] { "SessionId", "QuestionIndex", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldenBellQuestions_Difficulty_QuestionType",
                schema: "wordy",
                table: "GoldenBellQuestions",
                columns: new[] { "Difficulty", "QuestionType" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldenBellSessions_ChildProfileId_StartedAt",
                schema: "wordy",
                table: "GoldenBellSessions",
                columns: new[] { "ChildProfileId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoldenBellAttempts",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "GoldenBellQuestions",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "GoldenBellSessions",
                schema: "wordy");
        }
    }
}
