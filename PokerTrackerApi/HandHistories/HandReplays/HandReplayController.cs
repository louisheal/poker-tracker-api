using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandHistories.HandReplays;

[Controller]
[Route("api/handreplay")]
public class HandReplayController : ControllerBase
{
    private readonly HandReplayDto Basic = new(
        "#RC12345",
        new HoleCardsDto(new PlayingCardDto("A", "d"), new PlayingCardDto("A", "s")),
        "BTN",
        new Dictionary<string, decimal>
        {
            ["LJ"] = 100m,
            ["HJ"] = 100m,
            ["CO"] = 100m,
            ["BTN"] = 100m,
            ["SB"] = 100m,
            ["BB"] = 100m,
        },
        [
            new HandReplaySpotDto(
                0m,
                ["LJ", "HJ", "CO", "BTN", "SB", "BB"],
                "LJ",
                new Dictionary<string, decimal> { ["SB"] = 0.5m, ["BB"] = 1m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 100m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["HJ", "CO", "BTN", "SB", "BB"],
                "HJ",
                new Dictionary<string, decimal> { ["SB"] = 0.5m, ["BB"] = 1m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 100m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["CO", "BTN", "SB", "BB"],
                "CO",
                new Dictionary<string, decimal> { ["SB"] = 0.5m, ["BB"] = 1m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 100m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["BTN", "SB", "BB"],
                "BTN",
                new Dictionary<string, decimal> { ["SB"] = 0.5m, ["BB"] = 1m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 100m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["BTN", "SB", "BB"],
                "SB",
                new Dictionary<string, decimal>
                {
                    ["SB"] = 0.5m,
                    ["BB"] = 1m,
                    ["BTN"] = 2.5m,
                },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 97.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["BTN", "BB"],
                "BB",
                new Dictionary<string, decimal>
                {
                    ["SB"] = 0.5m,
                    ["BB"] = 1m,
                    ["BTN"] = 2.5m,
                },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 97.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 99m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                0m,
                ["BTN", "BB"],
                null,
                new Dictionary<string, decimal>
                {
                    ["SB"] = 0.5m,
                    ["BB"] = 2.5m,
                    ["BTN"] = 2.5m,
                },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 97.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 97.5m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                5.5m,
                ["BTN", "BB"],
                "BB",
                new Dictionary<string, decimal> { },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 97.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 97.5m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [
                    new PlayingCardDto("K", "s"),
                    new PlayingCardDto("7", "c"),
                    new PlayingCardDto("2", "h"),
                ],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                5.5m,
                ["BTN", "BB"],
                "BTN",
                new Dictionary<string, decimal> { },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 97.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 97.5m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [
                    new PlayingCardDto("K", "s"),
                    new PlayingCardDto("7", "c"),
                    new PlayingCardDto("2", "h"),
                ],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                5.5m,
                ["BTN", "BB"],
                "BB",
                new Dictionary<string, decimal> { ["BTN"] = 2m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 95.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 97.5m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [
                    new PlayingCardDto("K", "s"),
                    new PlayingCardDto("7", "c"),
                    new PlayingCardDto("2", "h"),
                ],
                new Dictionary<string, decimal> { }
            ),
            new HandReplaySpotDto(
                5.5m,
                ["BTN"],
                null,
                new Dictionary<string, decimal> { ["BTN"] = 2m },
                new Dictionary<string, decimal>
                {
                    ["LJ"] = 100m,
                    ["HJ"] = 100m,
                    ["CO"] = 100m,
                    ["BTN"] = 95.5m,
                    ["SB"] = 99.5m,
                    ["BB"] = 97.5m,
                },
                new Dictionary<string, HoleCardsDto> { },
                "Preflop",
                [
                    new PlayingCardDto("K", "s"),
                    new PlayingCardDto("7", "c"),
                    new PlayingCardDto("2", "h"),
                ],
                new Dictionary<string, decimal> { }
            ),
        ]
    );

    [HttpGet]
    public async Task<HandReplayDto> GetHandReplay()
    {
        return Basic;
    }
}
