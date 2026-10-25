using System.Text;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public partial class PostflopBettingSpotExtractor
{
    private class ExtractorState
    {
        private readonly List<ActionObservation> _observations = [];
        private readonly Dictionary<string, decimal> _streetContributions = new(
            StringComparer.Ordinal
        );
        private readonly HashSet<string> _checkedPlayers = new(StringComparer.Ordinal);
        private readonly Dictionary<PokerStreet, List<char>> _streetActions = [];
        private readonly Dictionary<string, PokerHandPlayer> _players;
        private readonly HashSet<string> _activePlayers;
        private readonly StringBuilder _actionPath = new();

        private PendingBet? _pendingBet;
        private PokerHandFlopDealtEvent? _flop;
        private PlayingCard? _turnCard;
        private PostflopRunout? _turnRunout;
        private PostflopRunout? _riverRunout;
        private int _actionOrdinal;
        private bool _eligible;

        public ExtractorState(PokerHand hand)
        {
            HandId = hand.HandId;
            HeroPlayerId = hand.HeroPlayerId;
            _players = hand.Players.ToDictionary(player => player.PlayerId);
            _activePlayers = _players.Keys.ToHashSet(StringComparer.Ordinal);
        }

        public string HandId { get; }
        public string HeroPlayerId { get; }
        public decimal PotAmountBB { get; private set; }
        public PokerStreet Street { get; private set; } = PokerStreet.Preflop;
        public int PreflopRaiseCount { get; private set; }
        public string? PfrPlayerId { get; private set; }
        public string? DefendingPlayerId { get; private set; }
        public Rank? FlopHighCard { get; private set; }
        public FlopTexture? FlopTextureValue { get; private set; }
        public FlopRankTexture? FlopRankTextureValue { get; private set; }
        public bool PfrInPosition { get; private set; }

        public bool Handle(PokerHandEvent handEvent) => HandleEvent(handEvent).IsValid;

        private HandlerResult HandleEvent(PokerHandEvent handEvent) =>
            handEvent switch
            {
                PokerHandPostEvent post => HandlePost(post),
                PokerHandBoardDealtEvent board => HandleBoardDealt(board),
                PokerHandPlayerActionEvent action => HandlePlayerAction(action),
                PokerHandUncalledBetReturnedEvent returned => HandleUncalledBetReturned(returned),
                PokerHandCashDropEvent cashDrop => HandleCashDrop(cashDrop),
                _ => HandlerResult.Valid,
            };

        public IReadOnlyList<PostflopBettingSpot> BuildSpots()
        {
            if (!_eligible)
            {
                return [];
            }

            var flopActionSequence = GetActionSequence(PokerStreet.Flop);
            var turnActionSequence = GetActionSequence(PokerStreet.Turn);

            return _observations
                .Select(observation => new PostflopBettingSpot
                {
                    HandId = HandId,
                    ActionOrdinal = observation.ActionOrdinal,
                    HeroPlayerId = HeroPlayerId,
                    ActorPlayerId = observation.ActorPlayerId,
                    ActionPath = observation.ActionPath,
                    DecisionContext = observation.DecisionContext,
                    Action = observation.Action,
                    Street = observation.Street,
                    FlopHighCard = FlopHighCard,
                    FlopTexture = FlopTextureValue,
                    FlopRankTexture = FlopRankTextureValue,
                    FlopActionSequence = flopActionSequence,
                    TurnActionSequence = observation.Street is PokerStreet.Turn or PokerStreet.River
                        ? turnActionSequence
                        : null,
                    TurnRunout = observation.Street is PokerStreet.Turn or PokerStreet.River
                        ? _turnRunout
                        : null,
                    RiverRunout = observation.Street == PokerStreet.River ? _riverRunout : null,
                    PfrPlayerId = PfrPlayerId!,
                    DefendingPlayerId = DefendingPlayerId!,
                    PfrPosition = _players[PfrPlayerId!].Position,
                    DefendingPosition = _players[DefendingPlayerId!].Position,
                    PreflopRaiseCount = PreflopRaiseCount,
                    PfrInPosition = PfrInPosition,
                    ResponseTo = observation.ResponseTo,
                    ResponseAction = observation.ResponseAction,
                    BetResponseLine = observation.BetResponseLine,
                    BetToPotRatio = observation.BetToPotRatio,
                    ActionSizeToPotRatio = observation.ActionSizeToPotRatio,
                })
                .ToArray();
        }

        private HandlerResult HandlePost(PokerHandPostEvent post)
        {
            PotAmountBB += post.AmountBB;
            if (post.PostType != PostType.Ante)
            {
                AddStreetContribution(post.PlayerId, post.AmountBB);
            }

            return HandlerResult.Valid;
        }

        private HandlerResult HandleBoardDealt(PokerHandBoardDealtEvent board)
        {
            _streetContributions.Clear();
            _checkedPlayers.Clear();
            _pendingBet = null;
            _actionPath.Clear();
            _actionOrdinal = 0;

            switch (board)
            {
                case PokerHandFlopDealtEvent flop:
                    return HandleFlopDealt(flop);
                case PokerHandTurnDealtEvent turn:
                    Street = PokerStreet.Turn;
                    _turnCard = turn.Card;
                    _turnRunout = GetTurnRunout(_flop!, turn.Card);
                    return HandlerResult.Valid;
                case PokerHandRiverDealtEvent river:
                    Street = PokerStreet.River;
                    _riverRunout = GetRiverRunout(_flop!, _turnCard!, river.Card);
                    return HandlerResult.Valid;
                default:
                    return HandlerResult.Valid;
            }
        }

        private HandlerResult HandleFlopDealt(PokerHandFlopDealtEvent flop)
        {
            if (_activePlayers.Count != 2)
            {
                return HandlerResult.Invalid;
            }

            if (PfrPlayerId is null || !_activePlayers.Contains(PfrPlayerId))
            {
                return HandlerResult.Invalid;
            }

            _flop = flop;
            FlopHighCard = new[] { flop.First.Rank, flop.Second.Rank, flop.Third.Rank }.Max();

            var suits = new[] { flop.First.Suit, flop.Second.Suit, flop.Third.Suit };
            FlopTextureValue = suits.Distinct().Count() switch
            {
                1 => FlopTexture.Monotone,
                2 => FlopTexture.TwoTone,
                3 => FlopTexture.Rainbow,
                _ => throw new InvalidOperationException("A flop must contain three cards."),
            };

            var ranks = new[] { flop.First.Rank, flop.Second.Rank, flop.Third.Rank };
            FlopRankTextureValue = ranks.Distinct().Count() switch
            {
                1 => FlopRankTexture.Trips,
                2 => FlopRankTexture.Paired,
                3 => FlopRankTexture.Unpaired,
                _ => throw new InvalidOperationException("A flop must contain three cards."),
            };

            DefendingPlayerId = _activePlayers.Single(playerId => playerId != PfrPlayerId);
            PfrInPosition =
                GetPostflopOrder(_players[PfrPlayerId].Position)
                > GetPostflopOrder(_players[DefendingPlayerId].Position);
            Street = PokerStreet.Flop;
            _eligible = true;
            return HandlerResult.Valid;
        }

        private HandlerResult HandlePlayerAction(PokerHandPlayerActionEvent action)
        {
            if (Street == PokerStreet.Preflop)
            {
                HandlePreflopAction(action);
                return HandlerResult.Valid;
            }

            if (
                !_eligible
                || action.PlayerId != PfrPlayerId && action.PlayerId != DefendingPlayerId
                || !TryGetAction(action, out var actionType, out var token)
            )
            {
                return HandlerResult.Valid;
            }

            var decisionContext = _actionPath.ToString();
            _actionPath.Append(token);
            var path = _actionPath.ToString();
            var potBeforeAction = PotAmountBB;
            var actionContribution = GetActionContribution(action);
            decimal? actionSizeToPotRatio =
                potBeforeAction > 0 && actionContribution > 0
                    ? actionContribution / potBeforeAction
                    : null;

            var response = GetBetResponse(action);
            _observations.Add(
                new ActionObservation(
                    ActionOrdinal: _actionOrdinal++,
                    ActorPlayerId: action.PlayerId,
                    Action: actionType,
                    Street: Street,
                    ActionPath: path,
                    DecisionContext: decisionContext,
                    ActionSizeToPotRatio: actionSizeToPotRatio,
                    ResponseTo: response.ResponseTo,
                    ResponseAction: response.ResponseAction,
                    BetResponseLine: response.BetResponseLine,
                    BetToPotRatio: response.BetToPotRatio
                )
            );

            if (action is not PokerHandPlayerFoldEvent)
            {
                AddStreetAction(token);
            }

            if (action is PokerHandPlayerCheckEvent)
            {
                _checkedPlayers.Add(action.PlayerId);
            }

            ApplyActionContribution(action, actionContribution);

            if (action is PokerHandPlayerBetEvent && _pendingBet is null)
            {
                decimal? ratio = potBeforeAction > 0 ? actionContribution / potBeforeAction : null;
                _pendingBet = new PendingBet(
                    action.PlayerId,
                    action.PlayerId == PfrPlayerId
                        ? PostflopResponseTo.PfrBet
                        : PostflopResponseTo.DonkBet,
                    ratio
                );
            }

            if (action is PokerHandPlayerFoldEvent fold)
            {
                _activePlayers.Remove(fold.PlayerId);
            }

            return HandlerResult.Valid;
        }

        private void HandlePreflopAction(PokerHandPlayerActionEvent action)
        {
            switch (action)
            {
                case PokerHandPlayerFoldEvent fold:
                    _activePlayers.Remove(fold.PlayerId);
                    break;
                case PokerHandPlayerBetEvent bet:
                    PreflopRaiseCount++;
                    PfrPlayerId = bet.PlayerId;
                    ApplyActionContribution(bet, bet.BetAmountBB);
                    break;
                case PokerHandPlayerRaiseEvent raise:
                    PreflopRaiseCount++;
                    PfrPlayerId = raise.PlayerId;
                    ApplyActionContribution(raise, GetActionContribution(raise));
                    break;
                case PokerHandPlayerCallEvent call:
                    ApplyActionContribution(call, call.CallAmountBB);
                    break;
            }
        }

        private HandlerResult HandleUncalledBetReturned(PokerHandUncalledBetReturnedEvent returned)
        {
            var returnedAmount = Math.Min(
                returned.AmountBB,
                GetStreetContribution(returned.PlayerId)
            );
            PotAmountBB = Math.Max(0m, PotAmountBB - returnedAmount);
            AddStreetContribution(returned.PlayerId, -returnedAmount);
            return HandlerResult.Valid;
        }

        private HandlerResult HandleCashDrop(PokerHandCashDropEvent cashDrop)
        {
            PotAmountBB = Math.Max(0m, PotAmountBB - cashDrop.AmountBB);
            return HandlerResult.Valid;
        }

        private ResponseDetails GetBetResponse(PokerHandPlayerActionEvent action)
        {
            if (
                _pendingBet is null
                || action.PlayerId == _pendingBet.BettorPlayerId
                || !TryGetResponseAction(action, out var responseAction)
            )
            {
                return ResponseDetails.None;
            }

            var line = _checkedPlayers.Contains(action.PlayerId)
                ? PostflopBetResponseLine.XBF
                : PostflopBetResponseLine.BF;
            var response = new ResponseDetails(
                _pendingBet.ResponseTo,
                responseAction,
                line,
                _pendingBet.BetToPotRatio
            );
            _pendingBet = null;
            return response;
        }

        private decimal GetActionContribution(PokerHandPlayerActionEvent action) =>
            action switch
            {
                PokerHandPlayerBetEvent bet => bet.BetAmountBB,
                PokerHandPlayerCallEvent call => call.CallAmountBB,
                PokerHandPlayerRaiseEvent raise => Math.Max(
                    0m,
                    raise.RaiseToAmountBB - GetStreetContribution(raise.PlayerId)
                ),
                _ => 0m,
            };

        private void ApplyActionContribution(
            PokerHandPlayerActionEvent action,
            decimal contribution
        )
        {
            PotAmountBB += contribution;
            if (contribution > 0)
            {
                AddStreetContribution(action.PlayerId, contribution);
            }
        }

        private void AddStreetAction(char token)
        {
            if (!_streetActions.TryGetValue(Street, out var actions))
            {
                actions = [];
                _streetActions[Street] = actions;
            }

            actions.Add(token);
        }

        private PostflopActionSequence? GetActionSequence(PokerStreet street)
        {
            if (!_streetActions.TryGetValue(street, out var actions))
            {
                return null;
            }

            return string.Concat(actions) switch
            {
                "XX" => PostflopActionSequence.XX,
                "XBC" => PostflopActionSequence.XBC,
                "XBRC" => PostflopActionSequence.XBRC,
                "BRC" => PostflopActionSequence.BRC,
                "BC" => PostflopActionSequence.BC,
                _ => null,
            };
        }

        private void AddStreetContribution(string playerId, decimal amount) =>
            _streetContributions[playerId] = GetStreetContribution(playerId) + amount;

        private decimal GetStreetContribution(string playerId) =>
            _streetContributions.TryGetValue(playerId, out var contribution) ? contribution : 0m;

        private static bool TryGetAction(
            PokerHandPlayerActionEvent action,
            out PostflopActionType actionType,
            out char token
        )
        {
            (actionType, token) = action switch
            {
                PokerHandPlayerCheckEvent => (PostflopActionType.Check, 'X'),
                PokerHandPlayerBetEvent => (PostflopActionType.Bet, 'B'),
                PokerHandPlayerRaiseEvent => (PostflopActionType.Raise, 'R'),
                PokerHandPlayerCallEvent => (PostflopActionType.Call, 'C'),
                PokerHandPlayerFoldEvent => (PostflopActionType.Fold, 'F'),
                _ => default,
            };

            return action
                is PokerHandPlayerCheckEvent
                    or PokerHandPlayerBetEvent
                    or PokerHandPlayerRaiseEvent
                    or PokerHandPlayerCallEvent
                    or PokerHandPlayerFoldEvent;
        }

        private static bool TryGetResponseAction(
            PokerHandPlayerActionEvent action,
            out PostflopResponseAction responseAction
        )
        {
            switch (action)
            {
                case PokerHandPlayerFoldEvent:
                    responseAction = PostflopResponseAction.Fold;
                    return true;
                case PokerHandPlayerCallEvent:
                    responseAction = PostflopResponseAction.Call;
                    return true;
                case PokerHandPlayerRaiseEvent:
                    responseAction = PostflopResponseAction.Raise;
                    return true;
                default:
                    responseAction = default;
                    return false;
            }
        }

        private static PostflopRunout GetTurnRunout(
            PokerHandFlopDealtEvent flop,
            PlayingCard turnCard
        )
        {
            var flopCards = new[] { flop.First, flop.Second, flop.Third };
            if (flopCards.All(card => turnCard.Rank > card.Rank))
            {
                return PostflopRunout.Overcard;
            }

            if (flopCards.Any(card => turnCard.Rank == card.Rank))
            {
                return PostflopRunout.Paired;
            }

            return flopCards.Count(card => turnCard.Suit == card.Suit) == 2
                ? PostflopRunout.FlushCompleting
                : PostflopRunout.Other;
        }

        private static PostflopRunout GetRiverRunout(
            PokerHandFlopDealtEvent flop,
            PlayingCard turnCard,
            PlayingCard riverCard
        )
        {
            var boardCards = new[] { flop.First, flop.Second, flop.Third, turnCard };
            if (boardCards.All(card => riverCard.Rank > card.Rank))
            {
                return PostflopRunout.Overcard;
            }

            if (boardCards.Any(card => riverCard.Rank == card.Rank))
            {
                return PostflopRunout.Paired;
            }

            return boardCards.Count(card => riverCard.Suit == card.Suit) >= 2
                ? PostflopRunout.FlushCompleting
                : PostflopRunout.Other;
        }

        private static int GetPostflopOrder(PokerPosition position) =>
            position switch
            {
                PokerPosition.SB => 0,
                PokerPosition.BB => 1,
                PokerPosition.LJ => 2,
                PokerPosition.HJ => 3,
                PokerPosition.CO => 4,
                PokerPosition.BTN => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
            };

        private record ActionObservation(
            int ActionOrdinal,
            string ActorPlayerId,
            PostflopActionType Action,
            PokerStreet Street,
            string ActionPath,
            string DecisionContext,
            decimal? ActionSizeToPotRatio,
            PostflopResponseTo? ResponseTo,
            PostflopResponseAction? ResponseAction,
            PostflopBetResponseLine? BetResponseLine,
            decimal? BetToPotRatio
        );

        private record PendingBet(
            string BettorPlayerId,
            PostflopResponseTo ResponseTo,
            decimal? BetToPotRatio
        );

        private record ResponseDetails(
            PostflopResponseTo? ResponseTo,
            PostflopResponseAction? ResponseAction,
            PostflopBetResponseLine? BetResponseLine,
            decimal? BetToPotRatio
        )
        {
            public static ResponseDetails None { get; } = new(null, null, null, null);
        }

        private record HandlerResult(bool IsValid)
        {
            public static HandlerResult Valid { get; } = new(true);
            public static HandlerResult Invalid { get; } = new(false);
        }
    }
}
