using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public class PostflopBettingSpotExtractorStateful
{
    public IReadOnlyList<PostflopBettingSpot> Extract(PokerHand hand)
    {
        var state = new ExtractorState(hand);
        var handEvents = hand.Events.OrderBy(handEvent => handEvent.Sequence);
        var spots = new List<PostflopBettingSpot>();

        foreach (var handEvent in handEvents)
        {
            var result = HandleHandEvent(state, handEvent);
            if (!result.IsValid)
            {
                break;
            }
            if (result.Spot != null)
            {
                spots.Add(result.Spot);
            }
        }

        return spots;
    }

    private HandlerResult HandleHandEvent(ExtractorState state, PokerHandEvent handEvent) =>
        handEvent switch
        {
            PokerHandSmallBlindPostEvent smallBlindPostEvent => HandleSmallBlindPostEvent(
                state,
                smallBlindPostEvent
            ),
            PokerHandBigBlindPostEvent bigBlindPostEvent => HandleBigBlindPostEvent(
                state,
                bigBlindPostEvent
            ),
            PokerHandPlayerFoldEvent playerFoldEvent => HandlePlayerFoldEvent(
                state,
                playerFoldEvent
            ),
            PokerHandPlayerCheckEvent playerCheckEvent => HandlePlayerCheckEvent(
                state,
                playerCheckEvent
            ),
            PokerHandPlayerBetEvent playerBetEvent => HandlePlayerBetEvent(state, playerBetEvent),
            PokerHandPlayerCallEvent playerCallEvent => HandlePlayerCallEvent(
                state,
                playerCallEvent
            ),
            PokerHandPlayerRaiseEvent playerRaiseEvent => HandlePlayerRaiseEvent(
                state,
                playerRaiseEvent
            ),
            PokerHandFlopDealtEvent flopDealtEvent => HandleFlopDealtEvent(state, flopDealtEvent),
            PokerHandTurnDealtEvent turnDealtEvent => HandleTurnDealtEvent(state, turnDealtEvent),
            PokerHandRiverDealtEvent riverDealtEvent => HandleRiverDealtEvent(
                state,
                riverDealtEvent
            ),
            _ => throw new NotImplementedException(),
        };

    // TODO : increase line length limit
    // TODO : validate that this happens before any other actions
    private HandlerResult HandleSmallBlindPostEvent(
        ExtractorState state,
        PokerHandSmallBlindPostEvent smallBlindPostEvent
    )
    {
        state.PotAmountBB += smallBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    // TODO : validate that this happens before any other actions
    private HandlerResult HandleBigBlindPostEvent(
        ExtractorState state,
        PokerHandBigBlindPostEvent bigBlindPostEvent
    )
    {
        state.PotAmountBB += bigBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerFoldEvent(
        ExtractorState state,
        PokerHandPlayerFoldEvent playerFoldEvent
    )
    {
        state.ActivePlayers.Remove(playerFoldEvent.PlayerId);

        if (state.PokerStreet != PokerStreet.Preflop) { }

        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerCheckEvent(
        ExtractorState state,
        PokerHandPlayerCheckEvent playerCheckEvent
    )
    {
        // skip hands where it limped through
        if (state.PokerStreet == PokerStreet.Preflop)
        {
            return HandlerResult.Invalid;
        }

        // TODO : move this logic into a Validate() method
        if (state.PfrPlayerId == null || state.DefendingPlayerId == null)
        {
            return HandlerResult.Invalid;
        }

        // TODO : make generate spot method
        var spot = new PostflopBettingSpot
        {
            HandId = state.HandId,
            HeroPlayerId = state.HeroPlayerId,
            PfrPlayerId = state.PfrPlayerId,
            DefendingPlayerId = state.DefendingPlayerId,
            Street = state.PokerStreet,
            FlopHighCard = state.FlopHighCardRank,
            FlopTexture = state.FlopTexture,
            FlopRankTexture = state.FlopRankTexture,
            FlopActionSequence = state.FlopActionSequence,
        };

        return HandlerResult.ValidWithSpot(spot);
    }

    private HandlerResult HandlePlayerBetEvent(
        ExtractorState state,
        PokerHandPlayerBetEvent playerBetEvent
    )
    {
        // only valid actions preflop are Fold/Call/Raise
        if (state.PokerStreet == PokerStreet.Preflop)
        {
            return HandlerResult.Invalid;
        }

        state.PotAmountBB += playerBetEvent.BetAmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerCallEvent(
        ExtractorState state,
        PokerHandPlayerCallEvent playerCallEvent
    )
    {
        state.PotAmountBB += playerCallEvent.CallAmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerRaiseEvent(
        ExtractorState state,
        PokerHandPlayerRaiseEvent playerRaiseEvent
    )
    {
        if (state.PokerStreet == PokerStreet.Preflop)
        {
            state.PreflopRaiseCount += 1;
            state.PfrPlayerId = playerRaiseEvent.PlayerId;
        }

        state.PotAmountBB += playerRaiseEvent.RaiseAmountBB;
        return HandlerResult.Valid;
    }

    // TODO : potentially split into helpers
    // they should exist in a separate static class to declutter this file
    private HandlerResult HandleFlopDealtEvent(
        ExtractorState state,
        PokerHandFlopDealtEvent flopDealtEvent
    )
    {
        if (state.ActivePlayers.Count != 2)
        {
            return HandlerResult.Invalid;
        }

        // TODO : move this into Validate()
        if (state.PfrPlayerId == null || !state.ActivePlayers.Contains(state.PfrPlayerId))
        {
            return HandlerResult.Invalid;
        }

        state.FlopHighCardRank = new[]
        {
            flopDealtEvent.First.Rank,
            flopDealtEvent.Second.Rank,
            flopDealtEvent.Third.Rank,
        }.Max();

        var distinctSuitCount = new[]
        {
            flopDealtEvent.First.Suit,
            flopDealtEvent.Second.Suit,
            flopDealtEvent.Third.Suit,
        }
            .Distinct()
            .Count();

        state.FlopTexture = distinctSuitCount switch
        {
            1 => FlopTexture.Monotone,
            2 => FlopTexture.TwoTone,
            3 => FlopTexture.Rainbow,
            _ => throw new InvalidOperationException("A flop must contain three cards."),
        };

        state.FlopRankTexture = new[]
        {
            flopDealtEvent.First.Rank,
            flopDealtEvent.Second.Rank,
            flopDealtEvent.Third.Rank,
        }
            .Distinct()
            .Count() switch
        {
            1 => FlopRankTexture.Trips,
            2 => FlopRankTexture.Paired,
            3 => FlopRankTexture.Unpaired,
            _ => throw new InvalidOperationException("A flop must contain three cards."),
        };

        state.DefendingPlayerId = state.ActivePlayers.Single(playerId =>
            playerId != state.PfrPlayerId
        );
        state.PokerStreet = PokerStreet.Flop;

        return HandlerResult.Valid;
    }

    private HandlerResult HandleTurnDealtEvent(
        ExtractorState state,
        PokerHandTurnDealtEvent turnDealtEvent
    )
    {
        state.PokerStreet = PokerStreet.Turn;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleRiverDealtEvent(
        ExtractorState state,
        PokerHandRiverDealtEvent riverDealtEvent
    )
    {
        state.PokerStreet = PokerStreet.River;
        return HandlerResult.Valid;
    }

    // TODO : move into own file
    private class ExtractorState
    {
        public ExtractorState(PokerHand hand)
        {
            HandId = hand.HandId;
            HeroPlayerId = hand.HeroPlayerId;
            ActivePlayers = hand
                .Players.Select(player => player.PlayerId)
                .ToHashSet(StringComparer.Ordinal);
        }

        public string HandId { get; }
        public string HeroPlayerId { get; }
        public decimal PotAmountBB { get; set; }
        public PokerStreet PokerStreet { get; set; } = PokerStreet.Preflop;
        public PostflopActionSequence? FlopActionSequence { get; set; }
        public HashSet<string> ActivePlayers { get; }
        public int PreflopRaiseCount { get; set; }
        public string? PfrPlayerId { get; set; }
        public string? DefendingPlayerId { get; set; }
        public Rank? FlopHighCardRank { get; set; }
        public FlopTexture? FlopTexture { get; set; }
        public FlopRankTexture? FlopRankTexture { get; set; }
    }

    private record HandlerResult(bool IsValid, PostflopBettingSpot? Spot)
    {
        public static HandlerResult Valid = new(true, null);
        public static HandlerResult Invalid = new(false, null);

        public static HandlerResult ValidWithSpot(PostflopBettingSpot spot) => new(true, spot);
    }
}
