using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddHandLabels : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "HandLabelAssignments",
                columns: table => new
                {
                    HandId = table
                        .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table
                        .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandLabelAssignments", x => new { x.HandId, x.Label });
                    table.ForeignKey(
                        name: "FK_HandLabelAssignments_PokerHands_HandId",
                        column: x => x.HandId,
                        principalTable: "PokerHands",
                        principalColumn: "HandId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_HandLabelAssignments_Label_HandId",
            table: "HandLabelAssignments",
            columns: new[] { "Label", "HandId" }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HandLabelAssignments");
    }
}
