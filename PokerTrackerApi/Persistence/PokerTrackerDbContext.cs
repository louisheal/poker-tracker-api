using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImport;
using PokerTrackerApi.HandReplays;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.Persistence;

public class PokerTrackerDbContext : DbContext
{
    public PokerTrackerDbContext(DbContextOptions<PokerTrackerDbContext> options)
        : base(options) { }

    public DbSet<RawHand> RawHands => Set<RawHand>();

    public DbSet<PokerHand> PokerHands => Set<PokerHand>();

    public DbSet<PokerHandPlayer> PokerHandPlayers => Set<PokerHandPlayer>();

    public DbSet<PokerHandEvent> PokerHandEvents => Set<PokerHandEvent>();

    public DbSet<HandHistorySummary> HandHistorySummaries => Set<HandHistorySummary>();

    public DbSet<PreflopSpot> PreflopSpots => Set<PreflopSpot>();

    public DbSet<HandReplay> HandReplays => Set<HandReplay>();

    public DbSet<HandReplayPlayer> HandReplayPlayers => Set<HandReplayPlayer>();

    public DbSet<HandReplayEvent> HandReplayEvents => Set<HandReplayEvent>();

    public DbSet<HandReplayEventCard> HandReplayEventCards => Set<HandReplayEventCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RawHand>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity.Property(hand => hand.RawText).HasColumnType("longtext").IsRequired();
        });

        modelBuilder.Entity<PokerHand>(entity =>
        {
            entity.HasKey(hand => hand.HandId);
            entity.Property(hand => hand.HandId).HasMaxLength(32);
            entity.Property(hand => hand.HeroPlayerId).HasMaxLength(255);
            entity
                .HasOne<RawHand>()
                .WithOne()
                .HasForeignKey<PokerHand>(hand => hand.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasMany(hand => hand.Players)
                .WithOne()
                .HasForeignKey("HandId")
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasMany(hand => hand.Events)
                .WithOne()
                .HasForeignKey("HandId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.ComplexProperty(
                hand => hand.HeroHoleCards,
                holeCards =>
                {
                    holeCards.ComplexProperty(cards => cards.First);
                    holeCards.ComplexProperty(cards => cards.Second);
                }
            );
        });

        modelBuilder.Entity<PokerHandPlayer>(entity =>
        {
            entity.HasKey("HandId", nameof(PokerHandPlayer.PlayerId));
            entity.Property<string>("HandId").HasMaxLength(32);
            entity.Property(player => player.PlayerId).HasMaxLength(255);
            entity.Property(player => player.Position).HasConversion<int>();
        });

        modelBuilder.Entity<PokerHandEvent>(entity =>
        {
            entity.HasKey("HandId", nameof(PokerHandEvent.Sequence));
            entity.Property<string>("HandId").HasMaxLength(32);
            entity.Property(handEvent => handEvent.Sequence).ValueGeneratedNever();
            entity
                .HasDiscriminator<string>("EventType")
                .HasValue<PokerHandPlayerFoldEvent>("PlayerFold")
                .HasValue<PokerHandPlayerCheckEvent>("PlayerCheck")
                .HasValue<PokerHandPlayerCallEvent>("PlayerCall")
                .HasValue<PokerHandPlayerBetEvent>("PlayerBet")
                .HasValue<PokerHandPlayerRaiseEvent>("PlayerRaise")
                .HasValue<PokerHandAntePostEvent>("AntePost")
                .HasValue<PokerHandSmallBlindPostEvent>("SmallBlindPost")
                .HasValue<PokerHandBigBlindPostEvent>("BigBlindPost")
                .HasValue<PokerHandFlopDealtEvent>("FlopDealt")
                .HasValue<PokerHandTurnDealtEvent>("TurnDealt")
                .HasValue<PokerHandRiverDealtEvent>("RiverDealt")
                .HasValue<PokerHandCardsShownEvent>("CardsShown")
                .HasValue<PokerHandUncalledBetReturnedEvent>("UncalledBetReturned")
                .HasValue<PokerHandPotAwardedEvent>("PotAwarded")
                .HasValue<PokerHandCashDropEvent>("CashDrop");
            entity.Property<string>("EventType").HasMaxLength(32);
        });

        modelBuilder.Entity<PokerHandPlayerActionEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.PlayerId).HasMaxLength(255);
        });

        modelBuilder.Entity<PokerHandPostEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.PlayerId).HasMaxLength(255);
            entity.Property(handEvent => handEvent.PostType).HasConversion<int>();
        });

        modelBuilder.Entity<PokerHandBoardDealtEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.Street).HasConversion<int>();
        });

        modelBuilder.Entity<PokerHandFlopDealtEvent>(entity =>
        {
            entity.ComplexProperty(handEvent => handEvent.First);
            entity.ComplexProperty(handEvent => handEvent.Second);
            entity.ComplexProperty(handEvent => handEvent.Third);
        });

        modelBuilder.Entity<PokerHandTurnDealtEvent>(entity =>
        {
            entity.ComplexProperty(handEvent => handEvent.Card);
        });

        modelBuilder.Entity<PokerHandRiverDealtEvent>(entity =>
        {
            entity.ComplexProperty(handEvent => handEvent.Card);
        });

        modelBuilder.Entity<PokerHandCardsShownEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.PlayerId).HasMaxLength(255);
            entity.ComplexProperty(
                handEvent => handEvent.HoleCards,
                holeCards =>
                {
                    holeCards.ComplexProperty(cards => cards.First);
                    holeCards.ComplexProperty(cards => cards.Second);
                }
            );
        });

        modelBuilder.Entity<PokerHandUncalledBetReturnedEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.PlayerId).HasMaxLength(255);
        });

        modelBuilder.Entity<PokerHandPotAwardedEvent>(entity =>
        {
            entity.Property(handEvent => handEvent.PlayerId).HasMaxLength(255);
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

        modelBuilder.Entity<HandReplayPlayer>(entity =>
        {
            entity.HasKey(player => new { player.HandId, player.PlayerId });
            entity.Property(player => player.HandId).HasMaxLength(32);
            entity.Property(player => player.Position).HasConversion<int>();
            entity
                .HasOne<HandReplay>()
                .WithMany(replay => replay.Players)
                .HasForeignKey(player => player.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandReplayEvent>(entity =>
        {
            entity.HasKey(replayEvent => new { replayEvent.HandId, replayEvent.Sequence });
            entity.Property(replayEvent => replayEvent.HandId).HasMaxLength(32);
            entity.Property(replayEvent => replayEvent.Street).HasMaxLength(16);
            entity.Property(replayEvent => replayEvent.EventType).HasMaxLength(64);
            entity.Property(replayEvent => replayEvent.PlayerId).HasMaxLength(255);
            entity
                .HasOne<HandReplay>()
                .WithMany(replay => replay.Events)
                .HasForeignKey(replayEvent => replayEvent.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandReplayEventCard>(entity =>
        {
            entity.HasKey(card => new
            {
                card.HandId,
                card.Sequence,
                card.CardIndex,
            });
            entity.Property(card => card.HandId).HasMaxLength(32);
            entity.ComplexProperty(card => card.Card);
            entity
                .HasOne<HandReplayEvent>()
                .WithMany(replayEvent => replayEvent.Cards)
                .HasForeignKey(card => new { card.HandId, card.Sequence })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandReplay>(entity =>
        {
            entity.HasKey(replay => replay.HandId);
            entity.Property(replay => replay.HandId).HasMaxLength(32);
            entity.Property(replay => replay.HeroPosition).HasConversion<int>();
            entity
                .HasOne<RawHand>()
                .WithOne()
                .HasForeignKey<HandReplay>(replay => replay.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ComplexProperty(
                replay => replay.HeroHoleCards,
                holeCards =>
                {
                    holeCards.ComplexProperty(cards => cards.First);
                    holeCards.ComplexProperty(cards => cards.Second);
                }
            );
        });
    }
}
