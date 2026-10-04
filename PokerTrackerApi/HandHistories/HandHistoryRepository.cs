using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;
using PokerTrackerApi.HandNotes;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryRepository
{
    Task<HandHistory[]> GetHandHistoriesAsync(
        CancellationToken cancellationToken,
        bool? heroSawFlop = null,
        IReadOnlyCollection<string>? labels = null,
        bool includeUnlabelled = false
    );
}

public class HandHistoryRepository : IHandHistoryRepository
{
    private const int MaxHandHistorySummaries = 100;
    private readonly PokerTrackerDbContext _dbContext;

    public HandHistoryRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HandHistory[]> GetHandHistoriesAsync(
        CancellationToken cancellationToken,
        bool? heroSawFlop = null,
        IReadOnlyCollection<string>? labels = null,
        bool includeUnlabelled = false
    )
    {
        var pokerHandsQuery = _dbContext.PokerHands.AsNoTracking();

        if (heroSawFlop.HasValue)
        {
            pokerHandsQuery = pokerHandsQuery.Where(hand =>
                hand.Events.OfType<PokerHandFlopDealtEvent>()
                    .Any(flop =>
                        !hand
                            .Events.OfType<PokerHandPlayerFoldEvent>()
                            .Any(fold =>
                                fold.PlayerId == hand.HeroPlayerId && fold.Sequence < flop.Sequence
                            )
                    ) == heroSawFlop.Value
            );
        }

        if (labels is { Count: > 0 } && includeUnlabelled)
        {
            var selectedLabels = labels.Distinct().ToList();
            pokerHandsQuery = pokerHandsQuery.Where(hand =>
                _dbContext.HandLabelAssignments.Any(assignment =>
                    assignment.HandId == hand.HandId && selectedLabels.Contains(assignment.Label)
                )
                || !_dbContext.HandLabelAssignments.Any(assignment =>
                    assignment.HandId == hand.HandId
                )
            );
        }
        else if (labels is { Count: > 0 })
        {
            var selectedLabels = labels.Distinct().ToList();
            pokerHandsQuery = pokerHandsQuery.Where(hand =>
                _dbContext.HandLabelAssignments.Any(assignment =>
                    assignment.HandId == hand.HandId && selectedLabels.Contains(assignment.Label)
                )
            );
        }
        else if (includeUnlabelled)
        {
            pokerHandsQuery = pokerHandsQuery.Where(hand =>
                !_dbContext.HandLabelAssignments.Any(assignment => assignment.HandId == hand.HandId)
            );
        }

        var pokerHands = await pokerHandsQuery
            .OrderBy(hand => hand.HandId)
            .Take(MaxHandHistorySummaries)
            .ToArrayAsync(cancellationToken);

        var handIds = pokerHands.Select(hand => hand.HandId).ToList();
        var assignments = await _dbContext
            .HandLabelAssignments.AsNoTracking()
            .Where(assignment => handIds.Contains(assignment.HandId))
            .ToArrayAsync(cancellationToken);
        var labelsByHandId = assignments
            .GroupBy(assignment => assignment.HandId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var notesByHandId = await _dbContext
            .HandNotes.AsNoTracking()
            .Where(note => handIds.Contains(note.HandId))
            .ToDictionaryAsync(note => note.HandId, note => note.Note, cancellationToken);

        return pokerHands
            .Select(hand =>
                ToHandHistory(
                    hand,
                    labelsByHandId.GetValueOrDefault(hand.HandId) ?? [],
                    notesByHandId.GetValueOrDefault(hand.HandId) ?? string.Empty
                )
            )
            .ToArray();
    }

    private static HandHistory ToHandHistory(
        PokerHand hand,
        HandLabelAssignment[] labels,
        string note
    ) => new(hand.HandId, hand.HeroHoleCards, labels, note);
}
