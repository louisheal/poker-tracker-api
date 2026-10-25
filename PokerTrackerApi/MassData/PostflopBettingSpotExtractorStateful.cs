using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public class PostflopBettingSpotExtractorStateful
{
    // metadata
    private string _handId = string.Empty;
    private string _heroPlayerId = string.Empty;

    // all streets
    private decimal _potAmountBB = 0m;
    private PokerStreet _pokerStreet = PokerStreet.Preflop;
    private PostflopActionSequence? _flopActionSequence;

    // preflop
    private HashSet<string> _activePlayers = [];
    private int _preflopRaiseCount = 0;
    private string? _pfrPlayerId;
    private string? _defendingPlayerId;

    // flop
    private Rank? _flopHighCardRank;
    private FlopTexture? _flopTexture;
    private FlopRankTexture? _flopRankTexture;

    public IEnumerable<PostflopBettingSpot> Extract(PokerHand hand)
    {
        InitialiseState(hand);

        var handEvents = hand.Events.OrderBy(handEvent => handEvent.Sequence);

        foreach (var handEvent in handEvents)
        {
            var result = HandleHandEvent(handEvent);
            if (!result.IsValid)
            {
                break;
            }
            if (result.Spot != null)
            {
                yield return result.Spot;
            }
        }
    }

    private HandlerResult HandleHandEvent(PokerHandEvent handEvent) =>
        handEvent switch
        {
            PokerHandSmallBlindPostEvent smallBlindPostEvent => HandleSmallBlindPostEvent(
                smallBlindPostEvent
            ),
            PokerHandBigBlindPostEvent bigBlindPostEvent => HandleBigBlindPostEvent(
                bigBlindPostEvent
            ),
            PokerHandPlayerFoldEvent playerFoldEvent => HandlePlayerFoldEvent(playerFoldEvent),
            PokerHandPlayerCheckEvent playerCheckEvent => HandlePlayerCheckEvent(playerCheckEvent),
            PokerHandPlayerBetEvent playerBetEvent => HandlePlayerBetEvent(playerBetEvent),
            PokerHandPlayerCallEvent playerCallEvent => HandlePlayerCallEvent(playerCallEvent),
            PokerHandPlayerRaiseEvent playerRaiseEvent => HandlePlayerRaiseEvent(playerRaiseEvent),
            PokerHandFlopDealtEvent flopDealtEvent => HandleFlopDealtEvent(flopDealtEvent),
            PokerHandTurnDealtEvent turnDealtEvent => HandleTurnDealtEvent(turnDealtEvent),
            PokerHandRiverDealtEvent riverDealtEvent => HandleRiverDealtEvent(riverDealtEvent),
            _ => throw new NotImplementedException(),
        };

    // TODO : increase line length limit
    // TODO : validate that this happens before any other actions
    private HandlerResult HandleSmallBlindPostEvent(
        PokerHandSmallBlindPostEvent smallBlindPostEvent
    )
    {
        _potAmountBB += smallBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    // TODO : validate that this happens before any other actions
    private HandlerResult HandleBigBlindPostEvent(PokerHandBigBlindPostEvent bigBlindPostEvent)
    {
        _potAmountBB += bigBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerFoldEvent(PokerHandPlayerFoldEvent playerFoldEvent)
    {
        _activePlayers.Remove(playerFoldEvent.PlayerId);
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerCheckEvent(PokerHandPlayerCheckEvent playerCheckEvent)
    {
        // skip hands where it limped through
        if (_pokerStreet == PokerStreet.Preflop || _pfrPlayerId == null || _defendingPlayerId == null)
        {
            return HandlerResult.Invalid;
        }

        // TODO : make generate spot method
        var spot = new PostflopBettingSpot
        {
            HandId = _handId,
            HeroPlayerId = _heroPlayerId,
            // TODO : add some sort of Validate() method
            // accepts a street and/or event and checks that the current state is valid
            PfrPlayerId = _pfrPlayerId,
            DefendingPlayerId = _defendingPlayerId,
            Street = _pokerStreet,
            FlopHighCard = _flopHighCardRank,
            FlopTexture = _flopTexture,
            FlopRankTexture = _flopRankTexture,
            FlopActionSequence = _flopActionSequence,
        };

        return HandlerResult.ValidWithSpot(spot);
    }

    private HandlerResult HandlePlayerBetEvent(PokerHandPlayerBetEvent playerBetEvent)
    {
        // only valid actions preflop are Fold/Call/Raise
        if (_pokerStreet == PokerStreet.Preflop)
        {
            return HandlerResult.Invalid;
        }

        _potAmountBB += playerBetEvent.BetAmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerCallEvent(PokerHandPlayerCallEvent playerCallEvent)
    {
        _potAmountBB += playerCallEvent.CallAmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerRaiseEvent(PokerHandPlayerRaiseEvent playerRaiseEvent)
    {
        if (_pokerStreet == PokerStreet.Preflop)
        {
            _preflopRaiseCount += 1;
            _pfrPlayerId = playerRaiseEvent.PlayerId;
        }

        _potAmountBB += playerRaiseEvent.RaiseAmountBB;
        return HandlerResult.Valid;
    }

    // TODO : potentially split into helpers
    private HandlerResult HandleFlopDealtEvent(PokerHandFlopDealtEvent flopDealtEvent)
    {
        if (_activePlayers.Count != 2 || _pfrPlayerId == null || _defendingPlayerId == null)
        {
            return HandlerResult.Invalid;
        }

        _flopHighCardRank = new[]
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

        _flopTexture = distinctSuitCount switch
        {
            1 => FlopTexture.Monotone,
            2 => FlopTexture.TwoTone,
            3 => FlopTexture.Rainbow,
            _ => throw new InvalidOperationException("A flop must contain three cards."),
        };

        _flopRankTexture = new[]
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

        _defendingPlayerId = _activePlayers.Single(playerId => playerId != _pfrPlayerId);
        _pokerStreet = PokerStreet.Flop;

        return HandlerResult.Valid;
    }

    private HandlerResult HandleTurnDealtEvent(PokerHandTurnDealtEvent turnDealtEvent)
    {
        _pokerStreet = PokerStreet.Turn;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleRiverDealtEvent(PokerHandRiverDealtEvent riverDealtEvent)
    {
        _pokerStreet = PokerStreet.River;
        return HandlerResult.Valid;
    }

    private void InitialiseState(PokerHand hand)
    {
        _handId = hand.HandId;
        _heroPlayerId = hand.HeroPlayerId;

        _pokerStreet = PokerStreet.Preflop;
        _activePlayers = hand
            .Players.Select(player => player.PlayerId)
            .ToHashSet(StringComparer.Ordinal);
        _potAmountBB = 0;
        _preflopRaiseCount = 0;
        _pfrPlayerId = null;
    }

    private record HandlerResult(bool IsValid, PostflopBettingSpot? Spot)
    {
        public static HandlerResult Valid = new(true, null);
        public static HandlerResult Invalid = new(false, null);

        public static HandlerResult ValidWithSpot(PostflopBettingSpot spot) => new(true, spot);
    }
}
