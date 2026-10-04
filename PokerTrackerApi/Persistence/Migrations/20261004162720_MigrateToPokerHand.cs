using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToPokerHand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandHistorySummaries");

            migrationBuilder.DropTable(
                name: "HandReplayEventCards");

            migrationBuilder.DropTable(
                name: "HandReplayPlayers");

            migrationBuilder.DropTable(
                name: "HandReplayEvents");

            migrationBuilder.DropTable(
                name: "HandReplays");

            migrationBuilder.CreateTable(
                name: "PokerHands",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeroPlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeroHoleCards_First_Rank = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_First_Suit = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_Second_Rank = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_Second_Suit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerHands", x => x.HandId);
                    table.ForeignKey(
                        name: "FK_PokerHands_RawHands_HandId",
                        column: x => x.HandId,
                        principalTable: "RawHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PokerHandEvents",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Street = table.Column<int>(type: "int", nullable: true),
                    First_Rank = table.Column<int>(type: "int", nullable: true),
                    First_Suit = table.Column<int>(type: "int", nullable: true),
                    Second_Rank = table.Column<int>(type: "int", nullable: true),
                    Second_Suit = table.Column<int>(type: "int", nullable: true),
                    Third_Rank = table.Column<int>(type: "int", nullable: true),
                    Third_Suit = table.Column<int>(type: "int", nullable: true),
                    Card_Rank = table.Column<int>(type: "int", nullable: true),
                    Card_Suit = table.Column<int>(type: "int", nullable: true),
                    PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HoleCards_First_Rank = table.Column<int>(type: "int", nullable: true),
                    HoleCards_First_Suit = table.Column<int>(type: "int", nullable: true),
                    HoleCards_Second_Rank = table.Column<int>(type: "int", nullable: true),
                    HoleCards_Second_Suit = table.Column<int>(type: "int", nullable: true),
                    AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PokerHandPlayerActionEvent_PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BetAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    CallAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    RaiseAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    RaiseToAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    IsAllIn = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    PokerHandPostEvent_PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostType = table.Column<int>(type: "int", nullable: true),
                    PokerHandPostEvent_AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PokerHandPotAwardedEvent_PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PokerHandPotAwardedEvent_AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PokerHandUncalledBetReturnedEvent_PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PokerHandUncalledBetReturnedEvent_AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerHandEvents", x => new { x.HandId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_PokerHandEvents_PokerHands_HandId",
                        column: x => x.HandId,
                        principalTable: "PokerHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PokerHandPlayers",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Position = table.Column<int>(type: "int", nullable: false),
                    StartingStackBB = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerHandPlayers", x => new { x.HandId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_PokerHandPlayers_PokerHands_HandId",
                        column: x => x.HandId,
                        principalTable: "PokerHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokerHandEvents");

            migrationBuilder.DropTable(
                name: "PokerHandPlayers");

            migrationBuilder.DropTable(
                name: "PokerHands");

            migrationBuilder.CreateTable(
                name: "HandHistorySummaries",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeroPosition = table.Column<int>(type: "int", nullable: false),
                    HoleCards_First_Rank = table.Column<int>(type: "int", nullable: false),
                    HoleCards_First_Suit = table.Column<int>(type: "int", nullable: false),
                    HoleCards_Second_Rank = table.Column<int>(type: "int", nullable: false),
                    HoleCards_Second_Suit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandHistorySummaries", x => x.HandId);
                    table.ForeignKey(
                        name: "FK_HandHistorySummaries_RawHands_HandId",
                        column: x => x.HandId,
                        principalTable: "RawHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HandReplays",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeroPosition = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_First_Rank = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_First_Suit = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_Second_Rank = table.Column<int>(type: "int", nullable: false),
                    HeroHoleCards_Second_Suit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandReplays", x => x.HandId);
                    table.ForeignKey(
                        name: "FK_HandReplays_RawHands_HandId",
                        column: x => x.HandId,
                        principalTable: "RawHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HandReplayEvents",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RaiseToAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Street = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandReplayEvents", x => new { x.HandId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandReplayEvents_HandReplays_HandId",
                        column: x => x.HandId,
                        principalTable: "HandReplays",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HandReplayPlayers",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Position = table.Column<int>(type: "int", nullable: false),
                    StartingStackBB = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandReplayPlayers", x => new { x.HandId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_HandReplayPlayers_HandReplays_HandId",
                        column: x => x.HandId,
                        principalTable: "HandReplays",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HandReplayEventCards",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CardIndex = table.Column<int>(type: "int", nullable: false),
                    Card_Rank = table.Column<int>(type: "int", nullable: false),
                    Card_Suit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandReplayEventCards", x => new { x.HandId, x.Sequence, x.CardIndex });
                    table.ForeignKey(
                        name: "FK_HandReplayEventCards_HandReplayEvents_HandId_Sequence",
                        columns: x => new { x.HandId, x.Sequence },
                        principalTable: "HandReplayEvents",
                        principalColumns: new[] { "HandId", "Sequence" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
