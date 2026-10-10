using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRetiredPostflopBettingMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PostflopBettingSpots_Street_PreflopRaiseCount_FlopWentCheckC~",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "DonkBetBb",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "FlopWentCheckCheck",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HandTimestamp",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HeroCalledVillainRiverBet",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HeroCalledVillainRiverRaise",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HeroResponseToVillainRiverBet",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HeroResponseToVillainRiverRaise",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "HeroRiverBetToPotRatio",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "PfrBetBb",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "ResponseAmountBb",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "RiverShowdownOutcome",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "RiverWentToShowdown",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainResponseToHeroRiverBet",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainRiverBet",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainRiverBetShowdownOutcome",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainRiverRaise",
                table: "PostflopBettingSpots");

            migrationBuilder.DropColumn(
                name: "VillainRiverRaiseShowdownOutcome",
                table: "PostflopBettingSpots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DonkBetBb",
                table: "PostflopBettingSpots",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FlopWentCheckCheck",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HandTimestamp",
                table: "PostflopBettingSpots",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "HeroCalledVillainRiverBet",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HeroCalledVillainRiverRaise",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HeroResponseToVillainRiverBet",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HeroResponseToVillainRiverRaise",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "HeroRiverBetToPotRatio",
                table: "PostflopBettingSpots",
                type: "decimal(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PfrBetBb",
                table: "PostflopBettingSpots",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResponseAmountBb",
                table: "PostflopBettingSpots",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiverShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "RiverWentToShowdown",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VillainResponseToHeroRiverBet",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "VillainRiverBet",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VillainRiverBetShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "VillainRiverRaise",
                table: "PostflopBettingSpots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VillainRiverRaiseShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostflopBettingSpots_Street_PreflopRaiseCount_FlopWentCheckC~",
                table: "PostflopBettingSpots",
                columns: new[] { "Street", "PreflopRaiseCount", "FlopWentCheckCheck" });
        }
    }
}
