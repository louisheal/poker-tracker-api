using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.HandHistories;

namespace PokerTrackerApi.Persistence;

public class PokerTrackerDbContext : DbContext
{
    public PokerTrackerDbContext(DbContextOptions<PokerTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<RawHand> RawHands => Set<RawHand>();

    public DbSet<ParsedHand> ParsedHands => Set<ParsedHand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RawHand>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity.Property(hand => hand.RawText).HasColumnType("longtext").IsRequired();
        });

        modelBuilder.Entity<ParsedHand>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity.HasOne<RawHand>()
                .WithOne()
                .HasForeignKey<ParsedHand>(hand => hand.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}