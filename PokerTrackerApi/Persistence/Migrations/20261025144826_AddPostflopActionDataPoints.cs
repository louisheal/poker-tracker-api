using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPostflopActionDataPoints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_HandId",
            table: "PostflopBettingSpots",
            column: "HandId"
        );

        migrationBuilder.DropPrimaryKey(
            name: "PK_PostflopBettingSpots",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.AddColumn<int>(
            name: "ActionOrdinal",
            table: "PostflopBettingSpots",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder
            .AddColumn<string>(
                name: "Action",
                table: "PostflopBettingSpots",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "ActionPath",
                table: "PostflopBettingSpots",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<decimal>(
            name: "ActionSizeToPotRatio",
            table: "PostflopBettingSpots",
            type: "decimal(12,6)",
            precision: 12,
            scale: 6,
            nullable: true
        );

        migrationBuilder
            .AddColumn<string>(
                name: "ActorPlayerId",
                table: "PostflopBettingSpots",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: ""
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AddColumn<string>(
                name: "DecisionContext",
                table: "PostflopBettingSpots",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddPrimaryKey(
            name: "PK_PostflopBettingSpots",
            table: "PostflopBettingSpots",
            columns: new[] { "HandId", "Street", "ActionOrdinal" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_Action",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "Action" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_Street_DecisionContext",
            table: "PostflopBettingSpots",
            columns: new[] { "Street", "DecisionContext" }
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_HandId",
            table: "PostflopBettingSpots"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_PostflopBettingSpots_HandId",
            table: "PostflopBettingSpots",
            column: "HandId"
        );

        migrationBuilder.DropPrimaryKey(
            name: "PK_PostflopBettingSpots",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_Action",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_Street_DecisionContext",
            table: "PostflopBettingSpots"
        );

        migrationBuilder.DropColumn(name: "ActionOrdinal", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "Action", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "ActionPath", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "ActionSizeToPotRatio", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "ActorPlayerId", table: "PostflopBettingSpots");

        migrationBuilder.DropColumn(name: "DecisionContext", table: "PostflopBettingSpots");

        migrationBuilder.AddPrimaryKey(
            name: "PK_PostflopBettingSpots",
            table: "PostflopBettingSpots",
            columns: new[] { "HandId", "Street" }
        );

        migrationBuilder.DropIndex(
            name: "IX_PostflopBettingSpots_HandId",
            table: "PostflopBettingSpots"
        );
    }
}
