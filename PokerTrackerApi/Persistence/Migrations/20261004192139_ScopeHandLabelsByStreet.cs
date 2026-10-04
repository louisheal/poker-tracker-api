using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeHandLabelsByStreet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM HandLabelAssignments;");

            migrationBuilder.CreateIndex(
                name: "IX_HandLabelAssignments_HandId",
                table: "HandLabelAssignments",
                column: "HandId"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_HandLabelAssignments",
                table: "HandLabelAssignments"
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "Street",
                    table: "HandLabelAssignments",
                    type: "varchar(16)",
                    maxLength: 16,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HandLabelAssignments",
                table: "HandLabelAssignments",
                columns: new[] { "HandId", "Street", "Label" }
            );

            migrationBuilder.DropIndex(
                name: "IX_HandLabelAssignments_HandId",
                table: "HandLabelAssignments"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM HandLabelAssignments;");

            migrationBuilder.CreateIndex(
                name: "IX_HandLabelAssignments_HandId",
                table: "HandLabelAssignments",
                column: "HandId"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_HandLabelAssignments",
                table: "HandLabelAssignments"
            );

            migrationBuilder.DropColumn(name: "Street", table: "HandLabelAssignments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HandLabelAssignments",
                table: "HandLabelAssignments",
                columns: new[] { "HandId", "Label" }
            );

            migrationBuilder.DropIndex(
                name: "IX_HandLabelAssignments_HandId",
                table: "HandLabelAssignments"
            );
        }
    }
}
