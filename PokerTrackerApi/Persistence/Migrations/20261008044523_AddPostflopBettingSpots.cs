using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPostflopBettingSpots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "PostflopBettingSpots",
                columns: table => new
                {
                    HandId = table
                        .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Street = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeroPlayerId = table
                        .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandTimestamp = table.Column<DateTimeOffset>(
                        type: "datetime(6)",
                        nullable: false
                    ),
                    PfrPlayerId = table
                        .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefendingPlayerId = table
                        .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PfrPosition = table
                        .Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefendingPosition = table
                        .Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreflopRaiseCount = table.Column<int>(type: "int", nullable: false),
                    PfrInPosition = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FlopWentCheckCheck = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PfrBetBb = table.Column<decimal>(
                        type: "decimal(12,4)",
                        precision: 12,
                        scale: 4,
                        nullable: true
                    ),
                    DonkBetBb = table.Column<decimal>(
                        type: "decimal(12,4)",
                        precision: 12,
                        scale: 4,
                        nullable: true
                    ),
                    ResponseTo = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseAction = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseAmountBb = table.Column<decimal>(
                        type: "decimal(12,4)",
                        precision: 12,
                        scale: 4,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostflopBettingSpots", x => new { x.HandId, x.Street });
                    table.ForeignKey(
                        name: "FK_PostflopBettingSpots_PokerHands_HandId",
                        column: x => x.HandId,
                        principalTable: "PokerHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_PreflopRaiseCount_FlopWentCheckC~",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "PreflopRaiseCount", "FlopWentCheckCheck" }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PostflopBettingSpots");
    }
}
