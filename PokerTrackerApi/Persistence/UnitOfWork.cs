namespace PokerTrackerApi.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void ClearTracking();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly PokerTrackerDbContext _dbContext;

    public UnitOfWork(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);

    public void ClearTracking() => _dbContext.ChangeTracker.Clear();
}