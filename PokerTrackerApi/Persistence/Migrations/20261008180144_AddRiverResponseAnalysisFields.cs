using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRiverResponseAnalysisFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AddColumn<string>(
                name: "HeroResponseToVillainRiverBet",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "HeroResponseToVillainRiverRaise",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<decimal>(
            name: "HeroRiverBetToPotRatio",
            table: "PostflopBettingSpots",
            type: "decimal(12,6)",
            precision: 12,
            scale: 6,
            nullable: true
        );

        migrationBuilder.AddColumn<bool>(
            name: "RiverWentToShowdown",
            table: "PostflopBettingSpots",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );

        migrationBuilder
            .AddColumn<string>(
                name: "VillainResponseToHeroRiverBet",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HeroResponseToVillainRiverBet",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(
            name: "HeroResponseToVillainRiverRaise",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(name: "HeroRiverBetToPotRatio", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "RiverWentToShowdown", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(
            name: "VillainResponseToHeroRiverBet",
            table: "PostflopBettingSpots"
        );
    }
}
