using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHoanTien.Migrations
{
    /// <inheritdoc />
    public partial class AddWordyWings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wordy");

            migrationBuilder.CreateTable(
                name: "Children",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AvatarKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AgeBand = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vocabulary",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    AudioKey = table.Column<string>(type: "text", nullable: false),
                    ImageKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabulary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worlds",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VietnameseName = table.Column<string>(type: "text", nullable: false),
                    Hero = table.Column<string>(type: "text", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    Goal = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mastery",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExposureCount = table.Column<int>(type: "integer", nullable: false),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    IncorrectCount = table.Column<int>(type: "integer", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextReviewAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mastery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mastery_Children_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalSchema: "wordy",
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    WorldId = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Mechanic = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsBoss = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    ContentVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Levels_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalSchema: "wordy",
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WrongAttempts = table.Column<int>(type: "integer", nullable: false),
                    HintCount = table.Column<int>(type: "integer", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_Children_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalSchema: "wordy",
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attempts_Levels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "wordy",
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Progress",
                schema: "wordy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BestStars = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BestDurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Progress_Children_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalSchema: "wordy",
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Progress_Levels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "wordy",
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ChildProfileId_CompletedAt",
                schema: "wordy",
                table: "Attempts",
                columns: new[] { "ChildProfileId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_LevelId",
                schema: "wordy",
                table: "Attempts",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_ParentUserId",
                schema: "wordy",
                table: "Children",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_WorldId_DisplayOrder",
                schema: "wordy",
                table: "Levels",
                columns: new[] { "WorldId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mastery_ChildProfileId_Term",
                schema: "wordy",
                table: "Mastery",
                columns: new[] { "ChildProfileId", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Progress_ChildProfileId_LevelId",
                schema: "wordy",
                table: "Progress",
                columns: new[] { "ChildProfileId", "LevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Progress_LevelId",
                schema: "wordy",
                table: "Progress",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabulary_Term",
                schema: "wordy",
                table: "Vocabulary",
                column: "Term",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempts",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Mastery",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Progress",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Vocabulary",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Children",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Levels",
                schema: "wordy");

            migrationBuilder.DropTable(
                name: "Worlds",
                schema: "wordy");
        }
    }
}
