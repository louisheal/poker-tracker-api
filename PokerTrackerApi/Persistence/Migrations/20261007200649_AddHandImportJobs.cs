using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class AddHandImportJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "HandImportJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(
                        type: "char(36)",
                        nullable: false,
                        collation: "ascii_general_ci"
                    ),
                    Status = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandImportJobs", x => x.JobId);
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .CreateTable(
                name: "HandImportJobFiles",
                columns: table => new
                {
                    FileId = table.Column<Guid>(
                        type: "char(36)",
                        nullable: false,
                        collation: "ascii_general_ci"
                    ),
                    JobId = table.Column<Guid>(
                        type: "char(36)",
                        nullable: false,
                        collation: "ascii_general_ci"
                    ),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FileName = table
                        .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StorageKey = table
                        .Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table
                        .Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(
                        type: "datetime(6)",
                        nullable: true
                    ),
                    HandsSaved = table.Column<int>(type: "int", nullable: false),
                    DuplicateHands = table.Column<int>(type: "int", nullable: false),
                    InvalidHands = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table
                        .Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandImportJobFiles", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_HandImportJobFiles_HandImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "HandImportJobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_HandImportJobFiles_JobId_Sequence",
            table: "HandImportJobFiles",
            columns: new[] { "JobId", "Sequence" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_HandImportJobFiles_Status_LeaseExpiresAt",
            table: "HandImportJobFiles",
            columns: new[] { "Status", "LeaseExpiresAt" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_HandImportJobs_Status_CreatedAt",
            table: "HandImportJobs",
            columns: new[] { "Status", "CreatedAt" }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HandImportJobFiles");

        migrationBuilder.DropTable(name: "HandImportJobs");
    }
}
