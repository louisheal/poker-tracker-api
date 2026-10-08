using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlopTextureToPostflopBettingSpots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlopTexture",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostflopBettingSpots_Street_FlopTexture",
                table: "PostflopBettingSpots",
                columns: new[] { "Street", "FlopTexture" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PostflopBettingSpots_Street_FlopTexture",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "FlopTexture",
                table: "PostflopBettingSpots");
        }
    }
}
