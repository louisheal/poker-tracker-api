using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameParsedHandToHandHistorySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ButtonSeat",
                table: "ParsedHands",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.RenameTable(name: "ParsedHands", newName: "HandHistorySummaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "HandHistorySummaries", newName: "ParsedHands");

            migrationBuilder.DropColumn(name: "ButtonSeat", table: "ParsedHands");
        }
    }
}
