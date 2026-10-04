using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandNotes;

public interface IHandNotesRepository
{
    Task<bool> HandExistsAsync(string handId, CancellationToken cancellationToken);
    Task ReplaceNoteAsync(string handId, string note, CancellationToken cancellationToken);
    Task ReplaceLabelsAsync(
        string handId,
        IReadOnlyCollection<HandLabelAssignment> assignments,
        CancellationToken cancellationToken
    );
}

public class HandNotesRepository : IHandNotesRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandNotesRepository(PokerTrackerDbContext dbContext)
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
        var existingNote = await _dbContext.HandNotes.SingleOrDefaultAsync(
            handNote => handNote.HandId == handId,
            cancellationToken
        );
        if (existingNote is null)
        {
            _dbContext.HandNotes.Add(new HandNote { HandId = handId, Note = note });
        }
        else
        {
            existingNote.Note = note;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
