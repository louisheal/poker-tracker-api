using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizePostflopBetResponseBuckets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RiverBetToPotRatio",
                table: "PostflopBettingSpots",
                newName: "BetToPotRatio");

            migrationBuilder.RenameColumn(
                name: "RiverBetResponseLine",
                table: "PostflopBettingSpots",
                newName: "BetResponseLine");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BetToPotRatio",
                table: "PostflopBettingSpots",
                newName: "RiverBetToPotRatio");

            migrationBuilder.RenameColumn(
                name: "BetResponseLine",
                table: "PostflopBettingSpots",
                newName: "RiverBetResponseLine");
        }
    }
}
