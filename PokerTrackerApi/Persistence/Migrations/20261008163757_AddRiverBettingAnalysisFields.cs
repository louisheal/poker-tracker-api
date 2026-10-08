using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRiverBettingAnalysisFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AddColumn<string>(
                name: "FlopActionSequence",
                table: "PostflopBettingSpots",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "FlopRankTexture",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<bool>(
            name: "HeroCalledVillainRiverBet",
            table: "PostflopBettingSpots",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );

        migrationBuilder.AddColumn<bool>(
            name: "HeroCalledVillainRiverRaise",
            table: "PostflopBettingSpots",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );

        migrationBuilder
            .AddColumn<string>(
                name: "RiverRunout",
                table: "PostflopBettingSpots",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "RiverShowdownOutcome",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "TurnActionSequence",
                table: "PostflopBettingSpots",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "TurnRunout",
                table: "PostflopBettingSpots",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<bool>(
            name: "VillainRiverBet",
            table: "PostflopBettingSpots",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );

        migrationBuilder.AddColumn<bool>(
            name: "VillainRiverRaise",
            table: "PostflopBettingSpots",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_FlopActionSequence",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "FlopActionSequence" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_FlopRankTexture",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "FlopRankTexture" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_RiverRunout",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "RiverRunout" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_TurnActionSequence",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "TurnActionSequence" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_TurnRunout",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "TurnRunout" }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_FlopActionSequence",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_FlopRankTexture",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_RiverRunout",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_TurnActionSequence",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_TurnRunout",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(name: "FlopActionSequence", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "FlopRankTexture", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(
            name: "HeroCalledVillainRiverBet",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(
            name: "HeroCalledVillainRiverRaise",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(name: "RiverRunout", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "RiverShowdownOutcome", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "TurnActionSequence", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "TurnRunout", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "VillainRiverBet", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "VillainRiverRaise", table: "PostflopBettingSpots");
    }
}
