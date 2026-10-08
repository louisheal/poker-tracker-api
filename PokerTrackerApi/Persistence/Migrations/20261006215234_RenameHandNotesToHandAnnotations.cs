using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class RenameHandNotesToHandAnnotations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_HandNotes_RawHands_HandId", table: "HandNotes");

        migrationBuilder.RenameTable(name: "HandNotes", newName: "HandAnnotations");

        migrationBuilder.AddForeignKey(
            name: "FK_HandAnnotations_RawHands_HandId",
            table: "HandAnnotations",
            column: "HandId",
            principalTable: "RawHands",
            principalColumn: "HandId",
            onDelete: ReferentialAction.Cascade
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_HandAnnotations_RawHands_HandId",
            table: "HandAnnotations"
        );

        migrationBuilder.RenameTable(name: "HandAnnotations", newName: "HandNotes");

        migrationBuilder.AddForeignKey(
            name: "FK_HandNotes_RawHands_HandId",
            table: "HandNotes",
            column: "HandId",
            principalTable: "RawHands",
            principalColumn: "HandId",
            onDelete: ReferentialAction.Cascade
        );
    }
}
