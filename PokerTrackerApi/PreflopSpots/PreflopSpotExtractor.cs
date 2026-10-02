using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.InternalRepresentation;
using PokerTrackerApi.Domain.InternalRepresentation.Events;

namespace PokerTrackerApi.PreflopSpots;

public sealed class PreflopSpotExtractor
{
    private static readonly string[] PositionNames =
    [
        "Lojack",
        "Hijack",
        "Cutoff",
        "Button",
        "Small Blind",
        "Big Blind",
    ];

    public IReadOnlyList<PreflopSpotObservation> Extract(ParsedHandIr hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var heroPosition = hand.Players[hand.HeroPlayerId].Position;
        var handKey = hand.HeroHoleCards.HandKey();
        var actions = new List<ObservedAction>();
        var observations = new List<PreflopSpotObservation>();

        foreach (var handEvent in hand.Events)
        {
            if (handEvent is BoardDealt)
            {
                break;
            }

            if (handEvent is not PlayerActionEvent playerAction)
            {
                continue;
            }

            if (!TryGetAction(playerAction, out var action, out var isAllIn))
            {
                break;
            }

            if (!hand.Players.TryGetValue(playerAction.PlayerId, out var player))
            {
                break;
            }

            if (playerAction.PlayerId == hand.HeroPlayerId)
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

            actions.Add(new ObservedAction(player.Position, action, isAllIn));
        }

        return observations;
    }

    private static bool TryGetAction(
        PlayerActionEvent playerAction,
        out PokerAction action,
        out bool isAllIn
    )
    {
        isAllIn = false;
        switch (playerAction)
        {
            case PlayerFoldEvent:
                action = PokerAction.Fold;
                return true;
            case PlayerCallEvent:
                action = PokerAction.Call;
                return true;
            case PlayerRaiseEvent raise:
                action = PokerAction.Raise;
                isAllIn = raise.IsAllIn;
                return true;
            default:
                action = default;
                return false;
        }
    }

    private static bool TryCreateSpotKey(
        PokerPosition heroPosition,
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
                heroPosition == PokerPosition.BB
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
                || GetPositionOrder(opener.Position) >= GetPositionOrder(heroPosition)
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
            "X"
            + string.Concat(
                actions.Select(action =>
                    $"_{PositionNames[GetPositionOrder(action.Position)]}_{action.Action}"
                )
            );
        return true;
    }

    private static int GetPositionOrder(PokerPosition position) =>
        position switch
        {
            PokerPosition.LJ => 0,
            PokerPosition.HJ => 1,
            PokerPosition.CO => 2,
            PokerPosition.BTN => 3,
            PokerPosition.SB => 4,
            PokerPosition.BB => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };

    private record ObservedAction(PokerPosition Position, PokerAction Action, bool IsAllIn);
}