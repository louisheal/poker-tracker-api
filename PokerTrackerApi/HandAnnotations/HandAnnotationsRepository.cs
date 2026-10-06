using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandAnnotations;

public interface IHandAnnotationsRepository
{
    Task<bool> HandExistsAsync(string handId, CancellationToken cancellationToken);
    Task ReplaceNoteAsync(string handId, string note, CancellationToken cancellationToken);
    Task SetFlaggedAsync(string handId, bool flagged, CancellationToken cancellationToken);
    Task ReplaceLabelsAsync(
        string handId,
        IReadOnlyCollection<HandLabelAssignment> assignments,
        CancellationToken cancellationToken
    );
}

public class HandAnnotationsRepository : IHandAnnotationsRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandAnnotationsRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HandExistsAsync(string handId, CancellationToken cancellationToken) =>
        _dbContext.PokerHands.AnyAsync(hand => hand.HandId == handId, cancellationToken);

    public async Task ReplaceNoteAsync(
        string handId,
        string note,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        var existingAnnotation = await _dbContext.HandAnnotations.SingleOrDefaultAsync(
            annotation => annotation.HandId == handId,
            cancellationToken
        );
        if (existingAnnotation is null)
        {
            _dbContext.HandAnnotations.Add(new HandAnnotation { HandId = handId, Note = note });
        }
        else
        {
            existingAnnotation.Note = note;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetFlaggedAsync(
        string handId,
        bool flagged,
        CancellationToken cancellationToken
    )
    {
        var annotation = await _dbContext.HandAnnotations.SingleOrDefaultAsync(
            item => item.HandId == handId,
            cancellationToken
        );
        if (annotation is null)
        {
            _dbContext.HandAnnotations.Add(
                new HandAnnotation
                {
                    HandId = handId,
                    Note = string.Empty,
                    Flagged = flagged,
                }
            );
        }
        else
        {
            annotation.Flagged = flagged;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceLabelsAsync(
        string handId,
        IReadOnlyCollection<HandLabelAssignment> assignments,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        await _dbContext
            .HandLabelAssignments.Where(assignment => assignment.HandId == handId)
            .ExecuteDeleteAsync(cancellationToken);
        _dbContext.HandLabelAssignments.AddRange(assignments);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
