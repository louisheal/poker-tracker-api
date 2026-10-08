using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTrackerApi.Persistence.Migrations;

/// <inheritdoc />
public partial class ReplaceButtonSeatWithHeroPosition : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "ButtonSeat",
            table: "HandHistorySummaries",
            newName: "HeroPosition"
        );

        migrationBuilder.Sql(
            """
            UPDATE `HandHistorySummaries` AS summary
            INNER JOIN `RawHands` AS raw ON raw.`HandId` = summary.`HandId`
            SET summary.`HeroPosition` = CASE MOD(
                CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(REGEXP_SUBSTR(raw.`RawText`, 'Seat [1-6]: Hero '), 'Seat ', -1), ':', 1) AS UNSIGNED)
                - CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(REGEXP_SUBSTR(raw.`RawText`, 'Seat #[1-6] is the button'), '#', -1), ' ', 1) AS UNSIGNED)
                + 6,
                6
            )
                WHEN 0 THEN 3
                WHEN 1 THEN 4
                WHEN 2 THEN 5
                WHEN 3 THEN 0
                WHEN 4 THEN 1
                WHEN 5 THEN 2
            END;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE `HandHistorySummaries` SET `HeroPosition` = 0;");

        migrationBuilder.RenameColumn(
            name: "HeroPosition",
            table: "HandHistorySummaries",
            newName: "ButtonSeat"
        );
    }
}
