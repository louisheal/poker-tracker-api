using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlopHighCardToPostflopSpots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlopHighCard",
                table: "PostflopBettingSpots",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostflopBettingSpots_Street_FlopHighCard",
                table: "PostflopBettingSpots",
                columns: new[] { "Street", "FlopHighCard" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PostflopBettingSpots_Street_FlopHighCard",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "FlopHighCard",
                table: "PostflopBettingSpots");
        }
    }
}
