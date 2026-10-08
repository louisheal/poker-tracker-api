using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRiverBetResponseLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RiverBetResponseLine",
                table: "PostflopBettingSpots",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RiverBetToPotRatio",
                table: "PostflopBettingSpots",
                type: "decimal(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RiverBetResponseLine",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "RiverBetToPotRatio",
                table: "PostflopBettingSpots");
        }
    }
}
