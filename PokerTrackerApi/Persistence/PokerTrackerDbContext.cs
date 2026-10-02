using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImport;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.Persistence;

public class PokerTrackerDbContext : DbContext
{
    public PokerTrackerDbContext(DbContextOptions<PokerTrackerDbContext> options)
        : base(options) { }

    public DbSet<RawHand> RawHands => Set<RawHand>();

    public DbSet<HandHistorySummary> HandHistorySummaries => Set<HandHistorySummary>();

    public DbSet<PreflopSpot> PreflopSpots => Set<PreflopSpot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RawHand>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity.Property(hand => hand.RawText).HasColumnType("longtext").IsRequired();
        });

        modelBuilder.Entity<HandHistorySummary>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity
                .HasOne<RawHand>()
                .WithOne()
                .HasForeignKey<HandHistorySummary>(hand => hand.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ComplexProperty(
                hand => hand.HoleCards,
                holeCards =>
                {
                    holeCards.ComplexProperty(cards => cards.First);
                    holeCards.ComplexProperty(cards => cards.Second);
                }
            );
        });

        modelBuilder.Entity<PreflopSpot>(entity =>
        {
            entity.HasKey(spot => new { spot.HandId, spot.SpotKey });
            entity.Property(spot => spot.HandId).HasMaxLength(32);
            entity.Property(spot => spot.SpotKey).HasMaxLength(256);
            entity.Property(spot => spot.HandKey).HasMaxLength(3);
            entity.Property(spot => spot.Action).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(spot => new
            {
                spot.SpotKey,
                spot.HandKey,
                spot.Action,
            });
            entity
                .HasOne<RawHand>()
                .WithMany()
                .HasForeignKey(spot => spot.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
