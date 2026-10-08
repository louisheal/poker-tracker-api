using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddHoleCardsToParsedHands : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "HoleCards_First_Rank",
            table: "ParsedHands",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "HoleCards_First_Suit",
            table: "ParsedHands",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "HoleCards_Second_Rank",
            table: "ParsedHands",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "HoleCards_Second_Suit",
            table: "ParsedHands",
            type: "int",
            nullable: false,
            defaultValue: 0
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HoleCards_First_Rank", table: "ParsedHands");

        migrationBuilder.DropColumn(name: "HoleCards_First_Suit", table: "ParsedHands");

        migrationBuilder.DropColumn(name: "HoleCards_Second_Rank", table: "ParsedHands");

        migrationBuilder.DropColumn(name: "HoleCards_Second_Suit", table: "ParsedHands");
    }
}
