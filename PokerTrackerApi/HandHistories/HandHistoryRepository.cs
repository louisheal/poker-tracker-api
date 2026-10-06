using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;
using PokerTrackerApi.HandAnnotations;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryRepository
{
    Task<HandHistory[]> GetHandHistoriesAsync(
        CancellationToken cancellationToken,
        bool? heroSawFlop = null,
        IReadOnlyCollection<string>? labels = null,
        bool includeUnlabelled = false,
        bool flaggedOnly = false
    );

    Task<HandHistory[]> GetHandHistoriesByIdsAsync(
        IReadOnlyCollection<string> handIds,
        CancellationToken cancellationToken
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
        bool includeUnlabelled = false,
        bool flaggedOnly = false
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

        if (flaggedOnly)
        {
            pokerHandsQuery = pokerHandsQuery.Where(hand =>
                _dbContext.HandAnnotations.Any(annotation =>
                    annotation.HandId == hand.HandId && annotation.Flagged
                )
            );
        }

        var pokerHands = await pokerHandsQuery
            .OrderByDescending(hand => hand.Timestamp)
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
        var annotationsByHandId = await _dbContext
            .HandAnnotations.AsNoTracking()
            .Where(annotation => handIds.Contains(annotation.HandId))
            .ToDictionaryAsync(
                annotation => annotation.HandId,
                annotation => new { annotation.Note, annotation.Flagged },
                cancellationToken
            );

        return pokerHands
            .Select(hand =>
                ToHandHistory(
                    hand,
                    labelsByHandId.GetValueOrDefault(hand.HandId) ?? [],
                    annotationsByHandId.GetValueOrDefault(hand.HandId)?.Note ?? string.Empty,
                    annotationsByHandId.GetValueOrDefault(hand.HandId)?.Flagged ?? false
                )
            )
            .ToArray();
    }

    public async Task<HandHistory[]> GetHandHistoriesByIdsAsync(
        IReadOnlyCollection<string> handIds,
        CancellationToken cancellationToken
    )
    {
        if (handIds.Count == 0)
        {
            return [];
        }

        var uniqueHandIds = handIds.Distinct().ToArray();
        var pokerHands = await _dbContext
            .PokerHands.AsNoTracking()
            .Where(hand => Enumerable.Contains(uniqueHandIds, hand.HandId))
            .OrderByDescending(hand => hand.Timestamp)
            .ToArrayAsync(cancellationToken);

        var foundHandIds = pokerHands.Select(hand => hand.HandId).ToArray();
        var assignments = await _dbContext
            .HandLabelAssignments.AsNoTracking()
            .Where(assignment => Enumerable.Contains(foundHandIds, assignment.HandId))
            .ToArrayAsync(cancellationToken);
        var labelsByHandId = assignments
            .GroupBy(assignment => assignment.HandId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var annotationsByHandId = await _dbContext
            .HandAnnotations.AsNoTracking()
            .Where(annotation => Enumerable.Contains(foundHandIds, annotation.HandId))
            .ToDictionaryAsync(
                annotation => annotation.HandId,
                annotation => new { annotation.Note, annotation.Flagged },
                cancellationToken
            );

        return pokerHands
            .Select(hand =>
                ToHandHistory(
                    hand,
                    labelsByHandId.GetValueOrDefault(hand.HandId) ?? [],
                    annotationsByHandId.GetValueOrDefault(hand.HandId)?.Note ?? string.Empty,
                    annotationsByHandId.GetValueOrDefault(hand.HandId)?.Flagged ?? false
                )
            )
            .ToArray();
    }

    private static HandHistory ToHandHistory(
        PokerHand hand,
        HandLabelAssignment[] labels,
        string note,
        bool flagged
    ) => new(hand.HandId, hand.HeroHoleCards, labels, note, flagged);
}
