using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandParsing;

public partial class GgPokerHandParser : IPokerHandParser
{
    // TODO : these feel like generic Poker concepts - not specific to GG
    private static readonly string[] PositionOrder =
    [
        "Lojack",
        "Hijack",
        "Cutoff",
        "Button",
        "Small Blind",
        "Big Blind",
    ];

    private static readonly string[] PositionByButtonOffset =
    [
        "Button",
        "Small Blind",
        "Big Blind",
        "Lojack",
        "Hijack",
        "Cutoff",
    ];

    [GeneratedRegex(
        @"(?m)^Dealt to Hero \[(?<first>[2-9TJQKA][cdhs]) (?<second>[2-9TJQKA][cdhs])\]\r?$"
    )]
    private static partial Regex HoleCardsLineRegex();

    [GeneratedRegex(@"(?m)^Table .+ 6-max Seat #(?<button>[1-6]) is the button\r?$")]
    private static partial Regex ButtonLineRegex();

    [GeneratedRegex(@"(?m)^Seat (?<seat>[1-6]): (?<player>.+?) \([^\r\n]*\)\r?$")]
    private static partial Regex SeatLineRegex();

    [GeneratedRegex(@"^(?<player>[^:]+): (?<action>.+)$")]
    private static partial Regex ActionLineRegex();

    public HandHistoryParseResult ParseHand(string rawHand)
    {
        if (!TryParseHand(rawHand, out var parsedHand, out var error))
        {
            return HandHistoryParseResult.Failed(error);
        }
        return HandHistoryParseResult.Parsed(parsedHand);
    }

    private static bool TryParseHand(
        string rawText,
        [NotNullWhen(true)] out HandParseData? hand,
        [NotNullWhen(false)] out string? error
    )
    {
        hand = null;
        error = null;

        var holeCardsMarker = rawText.IndexOf("*** HOLE CARDS ***", StringComparison.Ordinal);
        if (holeCardsMarker < 0)
        {
            error = "The hand is missing the hole-cards section.";
            return false;
        }

        var tableHeader = rawText[..holeCardsMarker];
        var buttonMatches = ButtonLineRegex().Matches(tableHeader);
        var seatMatches = SeatLineRegex().Matches(tableHeader);
        if (buttonMatches.Count != 1 || seatMatches.Count != 6)
        {
            error = "Expected one six-max button and six seat lines.";
            return false;
        }

        var buttonSeat = int.Parse(buttonMatches[0].Groups["button"].Value);
        var seenSeats = new HashSet<int>();
        var playersByName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match seatMatch in seatMatches)
        {
            var seat = int.Parse(seatMatch.Groups["seat"].Value);
            var name = seatMatch.Groups["player"].Value;
            var offset = (seat - buttonSeat + 6) % 6;
            if (!seenSeats.Add(seat) || !playersByName.TryAdd(name, PositionByButtonOffset[offset]))
            {
                error = "The hand contains duplicate seat data.";
                return false;
            }
        }

        if (!playersByName.TryGetValue("Hero", out var heroPosition))
        {
            error = "The hand does not contain a Hero seat.";
            return false;
        }

        if (!TryParseHoleCards(rawText, out var holeCards) || holeCards is null)
        {
            error = "Expected exactly one valid Hero hole-card line.";
            return false;
        }

        var actionStart = holeCardsMarker + "*** HOLE CARDS ***".Length;
        var actionEnd = rawText.IndexOf("***", actionStart, StringComparison.Ordinal);
        if (actionEnd < 0)
        {
            error = "The hand is missing the next section after hole cards.";
            return false;
        }

        var handKey = holeCards.HandKey();
        var actions = new List<ObservedAction>();
        var observations = new List<PreflopSpotObservation>();

        foreach (var line in rawText[actionStart..actionEnd].Split('\n'))
        {
            var actionMatch = ActionLineRegex().Match(line.TrimEnd('\r'));
            if (!actionMatch.Success)
            {
                continue;
            }

            var player = actionMatch.Groups["player"].Value;
            if (
                !playersByName.TryGetValue(player, out var position)
                || !TryParseAction(
                    actionMatch.Groups["action"].Value,
                    out var action,
                    out var isAllIn
                )
            )
            {
                break;
            }

            if (player == "Hero")
            {
                if (!TryCreateSpotKey(heroPosition, actions, out var spotKey, out var isRfi))
                {
                    break;
                }

                if (isRfi && action == PokerAction.Call)
                {
                    break;
                }

                observations.Add(new PreflopSpotObservation(spotKey, handKey, action));
            }

            actions.Add(new ObservedAction(position, action, isAllIn));
        }

        hand = new HandParseData(holeCards, observations);
        return true;
    }

    private static bool TryParseHoleCards(string rawHand, out HoleCards? holeCards)
    {
        var matches = HoleCardsLineRegex().Matches(rawHand);
        if (matches.Count != 1)
        {
            holeCards = null;
            return false;
        }

        var match = matches[0];
        holeCards = HoleCards.FromCodes(match.Groups["first"].Value, match.Groups["second"].Value);
        return true;
    }

    private static bool TryCreateSpotKey(
        string heroPosition,
        IReadOnlyList<ObservedAction> actions,
        out string spotKey,
        out bool isRfi
    )
    {
        spotKey = string.Empty;
        isRfi = false;

        if (actions.Any(action => action.Action == PokerAction.Call))
        {
            return false;
        }

        var raises = actions.Where(action => action.Action == PokerAction.Raise).ToArray();
        if (raises.Length == 0)
        {
            if (
                heroPosition == "Big Blind"
                || actions.Any(action => action.Action != PokerAction.Fold)
            )
            {
                return false;
            }

            isRfi = true;
        }
        else if (raises.Length == 1)
        {
            var opener = raises[0];
            if (
                opener.IsAllIn
                || opener.Position == "Big Blind"
                || PositionOrder.IndexOf(opener.Position) >= PositionOrder.IndexOf(heroPosition)
                || actions.Any(action => action != opener && action.Action != PokerAction.Fold)
            )
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        spotKey =
            "X" + string.Concat(actions.Select(action => $"_{action.Position}_{action.Action}"));
        return true;
    }

    private static bool TryParseAction(string text, out PokerAction action, out bool isAllIn)
    {
        isAllIn = text.Contains("is all-in", StringComparison.OrdinalIgnoreCase);
        if (text.StartsWith("folds", StringComparison.Ordinal))
        {
            action = PokerAction.Fold;
            return true;
        }

        if (text.StartsWith("calls", StringComparison.Ordinal))
        {
            action = PokerAction.Call;
            return true;
        }

        if (text.StartsWith("raises", StringComparison.Ordinal))
        {
            action = PokerAction.Raise;
            return true;
        }

        action = default;
        return false;
    }

    private record ObservedAction(string Position, PokerAction Action, bool IsAllIn);
}
