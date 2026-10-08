using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVillainRiverAggressionOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VillainRiverBetShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VillainRiverRaiseShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VillainRiverBetShowdownOutcome",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainRiverRaiseShowdownOutcome",
                table: "PostflopBettingSpots");
        }
    }
}
