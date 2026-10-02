using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHandReplayEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandReplayEvents",
                columns: table => new
                {
                    HandId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Street = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    RaiseToAmountBB = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandReplayEvents", x => new { x.HandId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandReplayEvents_RawHands_HandId",
                        column: x => x.HandId,
                        principalTable: "RawHands",
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
                        name: "FK_HandReplayPlayers_RawHands_HandId",
                        column: x => x.HandId,
                        principalTable: "RawHands",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandReplayEventCards");

            migrationBuilder.DropTable(
                name: "HandReplayPlayers");

            migrationBuilder.DropTable(
                name: "HandReplayEvents");
        }
    }
}
