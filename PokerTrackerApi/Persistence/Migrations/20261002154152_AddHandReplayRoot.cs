using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHandReplayRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HandReplayEvents_RawHands_HandId",
                table: "HandReplayEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_HandReplayPlayers_RawHands_HandId",
                table: "HandReplayPlayers");

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

            migrationBuilder.AddForeignKey(
                name: "FK_HandReplayEvents_HandReplays_HandId",
                table: "HandReplayEvents",
                column: "HandId",
                principalTable: "HandReplays",
                principalColumn: "HandId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HandReplayPlayers_HandReplays_HandId",
                table: "HandReplayPlayers",
                column: "HandId",
                principalTable: "HandReplays",
                principalColumn: "HandId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HandReplayEvents_HandReplays_HandId",
                table: "HandReplayEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_HandReplayPlayers_HandReplays_HandId",
                table: "HandReplayPlayers");

            migrationBuilder.DropTable(
                name: "HandReplays");

            migrationBuilder.AddForeignKey(
                name: "FK_HandReplayEvents_RawHands_HandId",
                table: "HandReplayEvents",
                column: "HandId",
                principalTable: "RawHands",
                principalColumn: "HandId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HandReplayPlayers_RawHands_HandId",
                table: "HandReplayPlayers",
                column: "HandId",
                principalTable: "RawHands",
                principalColumn: "HandId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
