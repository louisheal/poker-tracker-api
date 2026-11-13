using System.Globalization;
using System.Text.RegularExpressions;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Domain.HandHistories.Events;

namespace PokerTrackerApi.HandParsing;

public partial class GgPokerHandHistoryParser : IHandHistoryParser
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

    [GeneratedRegex(
        @"^Poker Hand #(?<id>[^:]+):.* - (?<timestamp>[0-9]{4}/[0-9]{2}/[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2})$"
    )]
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

    [GeneratedRegex(
        @"^Total pot \$(?<totalPot>[0-9,]+(?:\.[0-9]+)?) \| Rake \$(?<rake>[0-9,]+(?:\.[0-9]+)?)(?: \|.*)?$"
    )]
    private static partial Regex HandSummaryRegex();

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

    public HandHistory ParseHandHistory(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new FormatException("The hand text is empty.");
        }

        var lines = rawText.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var handHeader = HandHeaderRegex().Match(lines[0]);
        var blindLine = BlindLineRegex().Match(lines[0]);
        if (
            !handHeader.Success
            || !DateTimeOffset.TryParseExact(
                handHeader.Groups["timestamp"].Value,
                "yyyy/MM/dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var timestamp
            )
        )
        {
            throw new FormatException("The hand header or timestamp is invalid.");
        }

        if (
            !blindLine.Success
            || !TryParseCashAmount(blindLine.Groups["smallBlind"].Value, out var smallBlind)
            || !TryParseCashAmount(blindLine.Groups["bigBlind"].Value, out var bigBlind)
            || bigBlind <= 0
            || smallBlind < 0
        )
        {
            throw new FormatException("The hand header or blind values are invalid.");
        }

        var buttonMatches = ButtonLineRegex().Matches(rawText);
        var seatMatches = SeatLineRegex().Matches(rawText);
        if (buttonMatches.Count != 1 || seatMatches.Count != 6)
        {
            throw new FormatException("Expected one six-max button and six seat lines.");
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
            throw new FormatException("The button seat is invalid.");
        }

        var seenSeats = new HashSet<int>();
        var players = new Dictionary<string, HandHistoryPlayer>(StringComparer.Ordinal);
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
                throw new FormatException("A seat number or starting stack is invalid.");
            }

            var name = seatMatch.Groups["player"].Value;
            var offset = (seat - buttonSeat + 6) % 6;
            if (
                !seenSeats.Add(seat)
                || !players.TryAdd(
                    name,
                    new HandHistoryPlayer(name, PositionByButtonOffset[offset], startingStack / bigBlind)
                )
            )
            {
                throw new FormatException("The hand contains duplicate seat data.");
            }
        }

        if (!players.ContainsKey("Hero"))
        {
            throw new FormatException("The hand does not contain a Hero seat.");
        }

        var holeCardsMatches = HoleCardsLineRegex().Matches(rawText);
        if (holeCardsMatches.Count != 1)
        {
            throw new FormatException("Expected exactly one valid Hero hole-card line.");
        }

        var holeCardsMatch = holeCardsMatches[0];
        var holeCards = HoleCards.FromCodes(
            holeCardsMatch.Groups["first"].Value,
            holeCardsMatch.Groups["second"].Value
        );
        var events = new List<HandHistoryEvent>();
        decimal? totalPotBB = null;
        decimal? rakeBB = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("*** FLOP ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 3, out var cards))
                {
                    throw new FormatException(
                        "The flop section does not contain exactly three cards."
                    );
                }

                events.Add(
                    new FlopDealtEvent(
                        events.Count,
                        PokerStreet.Flop,
                        ParseCard(cards[0]),
                        ParseCard(cards[1]),
                        ParseCard(cards[2])
                    )
                );
                continue;
            }

            if (line.StartsWith("*** TURN ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 1, out var cards))
                {
                    throw new FormatException("The turn section does not contain one dealt card.");
                }

                events.Add(
                    new TurnDealtEvent(events.Count, PokerStreet.Turn, ParseCard(cards[0]))
                );
                continue;
            }

            if (line.StartsWith("*** RIVER ***", StringComparison.Ordinal))
            {
                if (!TryGetLastBracketedCards(line, 1, out var cards))
                {
                    throw new FormatException(
                        "The river section does not contain one dealt card."
                    );
                }

                events.Add(
                    new RiverDealtEvent(events.Count, PokerStreet.River, ParseCard(cards[0]))
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
                    throw new FormatException("An uncalled bet return is invalid.");
                }

                events.Add(
                    new UncalledBetReturnedEvent(
                        events.Count,
                        uncalledBet.Groups["player"].Value,
                        amountBB
                    )
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
                    throw new FormatException("A pot award is invalid.");
                }

                events.Add(
                    new PotAwardedEvent(
                        events.Count,
                        potAward.Groups["player"].Value,
                        amountBB
                    )
                );
                continue;
            }

            if (line.StartsWith("Total pot ", StringComparison.Ordinal))
            {
                var handSummary = HandSummaryRegex().Match(line);
                if (
                    !handSummary.Success
                    || !TryParseBigBlindAmount(
                        handSummary.Groups["totalPot"].Value,
                        bigBlind,
                        out var parsedTotalPotBB
                    )
                    || !TryParseBigBlindAmount(
                        handSummary.Groups["rake"].Value,
                        bigBlind,
                        out var parsedRakeBB
                    )
                    || parsedRakeBB > parsedTotalPotBB
                    || totalPotBB.HasValue
                )
                {
                    throw new FormatException("The hand summary pot or rake amount is invalid.");
                }

                totalPotBB = parsedTotalPotBB;
                rakeBB = parsedRakeBB;
                continue;
            }

            var cardsShown = CardsShownLineRegex().Match(line);
            if (cardsShown.Success)
            {
                var playerId = cardsShown.Groups["player"].Value;
                if (!players.ContainsKey(playerId))
                {
                    throw new FormatException("Shown cards refer to an unknown player.");
                }

                events.Add(
                    new CardsShownEvent(
                        events.Count,
                        playerId,
                        HoleCards.FromCodes(
                            cardsShown.Groups["first"].Value,
                            cardsShown.Groups["second"].Value
                        )
                    )
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

            events.Add(
                ParsePlayerAction(
                    actionMatch.Groups["player"].Value,
                    actionText,
                    bigBlind,
                    events.Count
                )
            );
        }

        if (!totalPotBB.HasValue || !rakeBB.HasValue)
        {
            throw new FormatException("The hand summary does not contain total pot and rake.");
        }

        return new HandHistory(
            handHeader.Groups["id"].Value,
            "Hero",
            holeCards,
            players.Values.ToList(),
            events,
            timestamp,
            totalPotBB.Value,
            rakeBB.Value
        );
    }

    private static HandHistoryEvent ParsePlayerAction(
        string playerId,
        string actionText,
        decimal bigBlind,
        int sequence
    )
    {
        if (actionText.StartsWith("folds", StringComparison.Ordinal))
        {
            return new PlayerFoldEvent(sequence, playerId);
        }

        if (actionText.StartsWith("checks", StringComparison.Ordinal))
        {
            return new PlayerCheckEvent(sequence, playerId);
        }

        var antePost = AntePostRegex().Match(actionText);
        if (antePost.Success)
        {
            if (!TryParseBigBlindAmount(antePost.Groups["amount"].Value, bigBlind, out var amount))
            {
                throw new FormatException("An ante post amount is invalid.");
            }

            return new AntePostEvent(sequence, playerId, PostType.Ante, amount);
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
                throw new FormatException("A small blind post amount is invalid.");
            }

            return new SmallBlindPostEvent(
                sequence,
                playerId,
                PostType.SmallBlind,
                amount
            );
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
                throw new FormatException("A big blind post amount is invalid.");
            }

            return new BigBlindPostEvent(
                sequence,
                playerId,
                PostType.BigBlind,
                amount
            );
        }

        var call = CallRegex().Match(actionText);
        if (call.Success)
        {
            if (!TryParseBigBlindAmount(call.Groups["amount"].Value, bigBlind, out var amount))
            {
                throw new FormatException("A call amount is invalid.");
            }

            return new PlayerCallEvent(sequence, playerId, amount);
        }

        var bet = BetRegex().Match(actionText);
        if (bet.Success)
        {
            if (!TryParseBigBlindAmount(bet.Groups["amount"].Value, bigBlind, out var amount))
            {
                throw new FormatException("A bet amount is invalid.");
            }

            return new PlayerBetEvent(sequence, playerId, amount);
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
                throw new FormatException("A raise amount is invalid.");
            }

            return new PlayerRaiseEvent(
                sequence,
                playerId,
                amount,
                raiseToAmount,
                raise
                    .Groups["suffix"]
                    .Value.Contains("is all-in", StringComparison.OrdinalIgnoreCase)
            );
        }

        throw new FormatException($"Unsupported player action: {playerId}: {actionText}");
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
