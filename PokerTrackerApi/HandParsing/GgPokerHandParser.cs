using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.HandParsing;

public partial class GgPokerHandParser : IPokerHandParser
{
    private static readonly PokerPosition[] PositionByButtonOffset =
    [
        PokerPosition.BTN,
        PokerPosition.SB,
        PokerPosition.BB,
        PokerPosition.LJ,
        PokerPosition.HJ,
        PokerPosition.CO,
    ];

    [GeneratedRegex(@"^Poker Hand #(?<id>[^:]+):.*$")]
    private static partial Regex HandHeaderRegex();

    [GeneratedRegex(
        @"Hold'em No Limit \(\$(?<smallBlind>[0-9]+(?:\.[0-9]+)?)/\$(?<bigBlind>[0-9]+(?:\.[0-9]+)?)\)"
    )]
    private static partial Regex BlindLineRegex();

    [GeneratedRegex(@"(?m)^Table .+ 6-max Seat #(?<button>[1-6]) is the button\r?$")]
    private static partial Regex ButtonLineRegex();

    [GeneratedRegex(
        @"(?m)^Seat (?<seat>[1-6]): (?<player>.+?) \(\$(?<stack>[0-9,]+(?:\.[0-9]+)?) in chips\)\r?$"
    )]
    private static partial Regex SeatLineRegex();

    [GeneratedRegex(
        @"(?m)^Dealt to Hero \[(?<first>[2-9TJQKA][cdhs]) (?<second>[2-9TJQKA][cdhs])\]\r?$"
    )]
    private static partial Regex HoleCardsLineRegex();

    [GeneratedRegex(@"^(?<player>[^:]+): (?<action>.+)$")]
    private static partial Regex ActionLineRegex();

    [GeneratedRegex(
        @"^(?<player>[^:]+): shows \[(?<first>[2-9TJQKA][cdhs]) (?<second>[2-9TJQKA][cdhs])\].*$"
    )]
    private static partial Regex CardsShownLineRegex();

    [GeneratedRegex(
        @"^Uncalled bet \(?\$(?<amount>[0-9,]+(?:\.[0-9]+)?)\)? returned to (?<player>.+)$"
    )]
    private static partial Regex UncalledBetRegex();

    [GeneratedRegex(
        @"^(?<player>.+?) collected \$(?<amount>[0-9,]+(?:\.[0-9]+)?) from (?:the )?(?:main )?pot$"
    )]
    private static partial Regex PotAwardedRegex();

    [GeneratedRegex(@"\[(?<cards>[^\]]+)\]")]
    private static partial Regex BracketedCardsRegex();

    [GeneratedRegex(@"(?:Rake|Jackpot|Bingo|Fortune|Tax) \$(?<amount>[0-9,]+(?:\.[0-9]+)?)")]
    private static partial Regex CashDropAmountRegex();

    [GeneratedRegex(@"^posts (?:the )?ante \$(?<amount>[0-9,]+(?:\.[0-9]+)?)(?: .*)?$")]
    private static partial Regex AntePostRegex();

    [GeneratedRegex(@"^posts (?:the )?small blind \$(?<amount>[0-9,]+(?:\.[0-9]+)?)(?: .*)?$")]
    private static partial Regex SmallBlindPostRegex();

    [GeneratedRegex(@"^posts (?:the )?big blind \$(?<amount>[0-9,]+(?:\.[0-9]+)?)(?: .*)?$")]
    private static partial Regex BigBlindPostRegex();

    [GeneratedRegex(@"^calls \$(?<amount>[0-9,]+(?:\.[0-9]+)?)(?: .*)?$")]
    private static partial Regex CallRegex();

    [GeneratedRegex(@"^bets \$(?<amount>[0-9,]+(?:\.[0-9]+)?)(?: .*)?$")]
    private static partial Regex BetRegex();

    [GeneratedRegex(
        @"^raises \$(?<amount>[0-9,]+(?:\.[0-9]+)?) to \$(?<raiseTo>[0-9,]+(?:\.[0-9]+)?)(?<suffix>.*)$"
    )]
    private static partial Regex RaiseRegex();

    public HandHistoryParseResult ParseHand(string rawHand)
    {
        if (!TryParseHand(rawHand, out var parsedHand, out var error))
        {
            return new HandHistoryParseFailure(error);
        }
        return new HandHistoryParseSuccess(parsedHand);
    }

    private static bool TryParseHand(
        string rawText,
        [NotNullWhen(true)] out PokerHand? hand,
        [NotNullWhen(false)] out string? error
    )
    {
        hand = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawText))
        {
            error = "The hand text is empty.";
            return false;
        }

        var lines = rawText.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var handHeader = HandHeaderRegex().Match(lines[0]);
        var blindLine = BlindLineRegex().Match(lines[0]);
        if (
            !handHeader.Success
            || !blindLine.Success
            || !TryParseCashAmount(blindLine.Groups["smallBlind"].Value, out var smallBlind)
            || !TryParseCashAmount(blindLine.Groups["bigBlind"].Value, out var bigBlind)
            || bigBlind <= 0
            || smallBlind < 0
        )
        {
            error = "The hand header or blind values are invalid.";
            return false;
        }

        var buttonMatches = ButtonLineRegex().Matches(rawText);
        var seatMatches = SeatLineRegex().Matches(rawText);
        if (buttonMatches.Count != 1 || seatMatches.Count != 6)
        {
            error = "Expected one six-max button and six seat lines.";
            return false;
        }

        if (
            !int.TryParse(
                buttonMatches[0].Groups["button"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var buttonSeat
            )
        )
        {
            error = "The button seat is invalid.";
            return false;
        }

        var seenSeats = new HashSet<int>();
        var players = new Dictionary<string, PokerHandPlayer>(StringComparer.Ordinal);
        foreach (Match seatMatch in seatMatches)
        {
            if (
                !int.TryParse(
                    seatMatch.Groups["seat"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var seat
                ) || !TryParseCashAmount(seatMatch.Groups["stack"].Value, out var startingStack)
            )
            {
                error = "A seat number or starting stack is invalid.";
                return false;
            }

            var name = seatMatch.Groups["player"].Value;
            var offset = (seat - buttonSeat + 6) % 6;
            if (
                !seenSeats.Add(seat)
                || !players.TryAdd(
                    name,
                    new PokerHandPlayer
                    {
                        PlayerId = name,
                        Position = PositionByButtonOffset[offset],
                        StartingStackBB = startingStack / bigBlind,
                    }
                )
            )
            {
                error = "The hand contains duplicate seat data.";
                return false;
            }
        }

        if (!players.ContainsKey("Hero"))
        {
            error = "The hand does not contain a Hero seat.";
            return false;
        }

        var holeCardsMatches = HoleCardsLineRegex().Matches(rawText);
        if (holeCardsMatches.Count != 1)
        {
            error = "Expected exactly one valid Hero hole-card line.";
            return false;
        }

        var holeCardsMatch = holeCardsMatches[0];
        var holeCards = HoleCards.FromCodes(
            holeCardsMatch.Groups["first"].Value,
            holeCardsMatch.Groups["second"].Value
        );
        var events = new List<PokerHandEvent>();

        foreach (var line in lines)
        {
            if (line.StartsWith("*** FLOP ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 3, out var cards))
                {
                    error = "The flop section does not contain exactly three cards.";
                    return false;
                }

                events.Add(
                    new PokerHandFlopDealtEvent
                    {
                        Sequence = events.Count,
                        Street = PokerStreet.Flop,
                        First = ParseCard(cards[0]),
                        Second = ParseCard(cards[1]),
                        Third = ParseCard(cards[2]),
                    }
                );
                continue;
            }

            if (line.StartsWith("*** TURN ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 1, out var cards))
                {
                    error = "The turn section does not contain one dealt card.";
                    return false;
                }

                events.Add(
                    new PokerHandTurnDealtEvent
                    {
                        Sequence = events.Count,
                        Street = PokerStreet.Turn,
                        Card = ParseCard(cards[0]),
                    }
                );
                continue;
            }

            if (line.StartsWith("*** RIVER ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 1, out var cards))
                {
                    error = "The river section does not contain one dealt card.";
                    return false;
                }

                events.Add(
                    new PokerHandRiverDealtEvent
                    {
                        Sequence = events.Count,
                        Street = PokerStreet.River,
                        Card = ParseCard(cards[0]),
                    }
                );
                continue;
            }

            var uncalledBet = UncalledBetRegex().Match(line);
            if (uncalledBet.Success)
            {
                if (
                    !players.ContainsKey(uncalledBet.Groups["player"].Value)
                    || !TryParseBigBlindAmount(
                        uncalledBet.Groups["amount"].Value,
                        bigBlind,
                        out var amountBB
                    )
                )
                {
                    error = "An uncalled bet return is invalid.";
                    return false;
                }

                events.Add(
                    new PokerHandUncalledBetReturnedEvent
                    {
                        Sequence = events.Count,
                        PlayerId = uncalledBet.Groups["player"].Value,
                        AmountBB = amountBB,
                    }
                );
                continue;
            }

            var potAward = PotAwardedRegex().Match(line);
            if (potAward.Success)
            {
                if (
                    !players.ContainsKey(potAward.Groups["player"].Value)
                    || !TryParseBigBlindAmount(
                        potAward.Groups["amount"].Value,
                        bigBlind,
                        out var amountBB
                    )
                )
                {
                    error = "A pot award is invalid.";
                    return false;
                }

                events.Add(
                    new PokerHandPotAwardedEvent
                    {
                        Sequence = events.Count,
                        PlayerId = potAward.Groups["player"].Value,
                        AmountBB = amountBB,
                    }
                );
                continue;
            }

            if (line.StartsWith("Total pot ", StringComparison.Ordinal))
            {
                var cashDrop = CashDropAmountRegex()
                    .Matches(line)
                    .Select(match => match.Groups["amount"].Value)
                    .Select(amount => TryParseCashAmount(amount, out var parsed) ? parsed : -1m)
                    .ToArray();
                if (cashDrop.Any(amount => amount < 0))
                {
                    error = "A cash-drop amount is invalid.";
                    return false;
                }

                var totalCashDrop = cashDrop.Sum();
                if (totalCashDrop > 0)
                {
                    events.Add(
                        new PokerHandCashDropEvent
                        {
                            Sequence = events.Count,
                            AmountBB = totalCashDrop / bigBlind,
                        }
                    );
                }

                continue;
            }

            var cardsShown = CardsShownLineRegex().Match(line);
            if (cardsShown.Success)
            {
                var playerId = cardsShown.Groups["player"].Value;
                if (!players.ContainsKey(playerId))
                {
                    error = "Shown cards refer to an unknown player.";
                    return false;
                }

                events.Add(
                    new PokerHandCardsShownEvent
                    {
                        Sequence = events.Count,
                        PlayerId = playerId,
                        HoleCards = HoleCards.FromCodes(
                            cardsShown.Groups["first"].Value,
                            cardsShown.Groups["second"].Value
                        ),
                    }
                );
                continue;
            }

            var actionMatch = ActionLineRegex().Match(line);
            if (!actionMatch.Success || !players.ContainsKey(actionMatch.Groups["player"].Value))
            {
                continue;
            }

            var actionText = actionMatch.Groups["action"].Value;
            if (
                actionText == "Chooses to EV Cashout"
                || actionText.StartsWith("Pays Cashout Risk", StringComparison.Ordinal)
            )
            {
                continue;
            }

            if (actionText.StartsWith("mucks", StringComparison.Ordinal))
            {
                continue;
            }

            if (
                !TryParsePlayerAction(
                    actionMatch.Groups["player"].Value,
                    actionText,
                    bigBlind,
                    events.Count,
                    out var playerAction
                )
            )
            {
                error = $"Unsupported player action: {line}";
                return false;
            }

            events.Add(playerAction);
        }

        hand = new PokerHand
        {
            HandId = handHeader.Groups["id"].Value,
            HeroPlayerId = "Hero",
            HeroHoleCards = holeCards,
            Players = players.Values.ToList(),
            Events = events,
        };
        return true;
    }

    private static bool TryParsePlayerAction(
        string playerId,
        string actionText,
        decimal bigBlind,
        int sequence,
        [NotNullWhen(true)] out PokerHandEvent? handEvent
    )
    {
        handEvent = null;
        if (actionText.StartsWith("folds", StringComparison.Ordinal))
        {
            handEvent = new PokerHandPlayerFoldEvent { Sequence = sequence, PlayerId = playerId };
            return true;
        }

        if (actionText.StartsWith("checks", StringComparison.Ordinal))
        {
            handEvent = new PokerHandPlayerCheckEvent { Sequence = sequence, PlayerId = playerId };
            return true;
        }

        var antePost = AntePostRegex().Match(actionText);
        if (antePost.Success)
        {
            if (!TryParseBigBlindAmount(antePost.Groups["amount"].Value, bigBlind, out var amount))
            {
                return false;
            }

            handEvent = new PokerHandAntePostEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                PostType = PostType.Ante,
                AmountBB = amount,
            };
            return true;
        }

        var smallBlindPost = SmallBlindPostRegex().Match(actionText);
        if (smallBlindPost.Success)
        {
            if (
                !TryParseBigBlindAmount(
                    smallBlindPost.Groups["amount"].Value,
                    bigBlind,
                    out var amount
                )
            )
            {
                return false;
            }

            handEvent = new PokerHandSmallBlindPostEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                PostType = PostType.SmallBlind,
                AmountBB = amount,
            };
            return true;
        }

        var bigBlindPost = BigBlindPostRegex().Match(actionText);
        if (bigBlindPost.Success)
        {
            if (
                !TryParseBigBlindAmount(
                    bigBlindPost.Groups["amount"].Value,
                    bigBlind,
                    out var amount
                )
            )
            {
                return false;
            }

            handEvent = new PokerHandBigBlindPostEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                PostType = PostType.BigBlind,
                AmountBB = amount,
            };
            return true;
        }

        var call = CallRegex().Match(actionText);
        if (call.Success)
        {
            if (!TryParseBigBlindAmount(call.Groups["amount"].Value, bigBlind, out var amount))
            {
                return false;
            }

            handEvent = new PokerHandPlayerCallEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                CallAmountBB = amount,
            };
            return true;
        }

        var bet = BetRegex().Match(actionText);
        if (bet.Success)
        {
            if (!TryParseBigBlindAmount(bet.Groups["amount"].Value, bigBlind, out var amount))
            {
                return false;
            }

            handEvent = new PokerHandPlayerBetEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                BetAmountBB = amount,
            };
            return true;
        }

        var raise = RaiseRegex().Match(actionText);
        if (raise.Success)
        {
            if (
                !TryParseBigBlindAmount(raise.Groups["amount"].Value, bigBlind, out var amount)
                || !TryParseBigBlindAmount(
                    raise.Groups["raiseTo"].Value,
                    bigBlind,
                    out var raiseToAmount
                )
            )
            {
                return false;
            }

            handEvent = new PokerHandPlayerRaiseEvent
            {
                Sequence = sequence,
                PlayerId = playerId,
                RaiseAmountBB = amount,
                RaiseToAmountBB = raiseToAmount,
                IsAllIn = raise
                    .Groups["suffix"]
                    .Value.Contains("is all-in", StringComparison.OrdinalIgnoreCase),
            };
            return true;
        }

        return false;
    }

    private static bool TryGetLastBracketedCards(string line, int expectedCount, out string[] cards)
    {
        var matches = BracketedCardsRegex().Matches(line);
        if (matches.Count == 0)
        {
            cards = [];
            return false;
        }

        cards = matches[^1].Groups["cards"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return cards.Length == expectedCount && cards.All(IsCardCode);
    }

    private static bool IsCardCode(string cardCode) =>
        cardCode.Length == 2
        && "23456789TJQKA".IndexOf(cardCode[0]) >= 0
        && "cdhs".IndexOf(cardCode[1]) >= 0;

    private static PlayingCard ParseCard(string cardCode) =>
        new(RankExtensions.FromCode(cardCode[0]), SuitExtensions.FromCode(cardCode[1]));

    private static bool TryParseCashAmount(string value, out decimal amount) =>
        decimal.TryParse(
            value.Replace(",", string.Empty, StringComparison.Ordinal),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount
        );

    private static bool TryParseBigBlindAmount(
        string cashAmount,
        decimal bigBlind,
        out decimal amountBB
    )
    {
        amountBB = 0;
        if (!TryParseCashAmount(cashAmount, out var amount))
        {
            return false;
        }

        amountBB = amount / bigBlind;
        return true;
    }
}
