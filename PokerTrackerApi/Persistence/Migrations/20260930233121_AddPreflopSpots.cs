using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreflopSpots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "PreflopSpots",
                    columns: table => new
                    {
                        HandId = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        SpotKey = table
                            .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        HandKey = table
                            .Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Action = table
                            .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_PreflopSpots", x => new { x.HandId, x.SpotKey });
                        table.ForeignKey(
                            name: "FK_PreflopSpots_RawHands_HandId",
                            column: x => x.HandId,
                            principalTable: "RawHands",
                            principalColumn: "HandId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PreflopSpots_SpotKey_HandKey_Action",
                table: "PreflopSpots",
                columns: new[] { "SpotKey", "HandKey", "Action" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PreflopSpots");
        }
    }
}
