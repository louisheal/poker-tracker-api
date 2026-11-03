using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class RecreatePreflopSpots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PreflopSpots");

        migrationBuilder
            .CreateTable(
                name: "PreflopSpots",
                columns: table => new
                {
                    HandId = table
                        .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorPosition = table.Column<int>(type: "int", nullable: false),
                    StateBeforeAction = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpotKey = table
                        .Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandKey = table
                        .Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpenerPosition = table.Column<int>(type: "int", nullable: true),
                    ThreeBettorPosition = table.Column<int>(type: "int", nullable: true),
                    IsHero = table.Column<bool>(type: "tinyint(1)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_PreflopSpots",
                        x => new { x.HandId, x.ActorPosition, x.StateBeforeAction }
                    );
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

        migrationBuilder.CreateIndex(
            name: "IX_PreflopSpots_StateBeforeAction_ActorPosition_OpenerPosition_~",
            table: "PreflopSpots",
            columns: new[]
            {
                "StateBeforeAction",
                "ActorPosition",
                "OpenerPosition",
                "ThreeBettorPosition",
                "Action",
                "HandKey",
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PreflopSpots");

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
}
