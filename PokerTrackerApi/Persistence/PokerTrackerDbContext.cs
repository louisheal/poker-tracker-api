using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;
using PokerTrackerApi.HandAnnotations;
using PokerTrackerApi.HandImporting;
using PokerTrackerApi.HandImporting.Jobs;
using PokerTrackerApi.MassData;
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

    public DbSet<PreflopSpot> PreflopSpots => Set<PreflopSpot>();

    public DbSet<HandLabelAssignment> HandLabelAssignments => Set<HandLabelAssignment>();

    public DbSet<HandAnnotation> HandAnnotations => Set<HandAnnotation>();

    public DbSet<HandImportJob> HandImportJobs => Set<HandImportJob>();

    public DbSet<HandImportJobFile> HandImportJobFiles => Set<HandImportJobFile>();

    public DbSet<PostflopBettingSpot> PostflopBettingSpots => Set<PostflopBettingSpot>();

    public DbSet<MassDataReprocessingJob> MassDataReprocessingJobs =>
        Set<MassDataReprocessingJob>();

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

        modelBuilder.Entity<HandLabelAssignment>(entity =>
        {
            entity.HasKey(assignment => new
            {
                assignment.HandId,
                assignment.Street,
                assignment.Label,
            });
            entity.Property(assignment => assignment.HandId).HasMaxLength(32);
            entity
                .Property(assignment => assignment.Street)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(assignment => assignment.Label).HasMaxLength(64);
            entity.HasIndex(assignment => new { assignment.Label, assignment.HandId });
            entity
                .HasOne<Domain.PokerHand.PokerHand>()
                .WithMany()
                .HasForeignKey(assignment => assignment.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandAnnotation>(entity =>
        {
            entity.HasKey(annotation => annotation.HandId);
            entity.Property(annotation => annotation.HandId).HasMaxLength(32);
            entity.Property(annotation => annotation.Note).HasColumnType("longtext").IsRequired();
            entity.Property(annotation => annotation.Flagged).HasDefaultValue(false);
            entity
                .HasOne<RawHand>()
                .WithMany()
                .HasForeignKey(annotation => annotation.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandImportJob>(entity =>
        {
            entity.HasKey(job => job.JobId);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
            entity
                .HasMany(job => job.Files)
                .WithOne()
                .HasForeignKey(file => file.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandImportJobFile>(entity =>
        {
            entity.HasKey(file => file.FileId);
            entity.Property(file => file.FileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.StorageKey).HasMaxLength(255).IsRequired();
            entity.Property(file => file.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(file => file.ErrorMessage).HasColumnType("longtext");
            entity.HasIndex(file => new { file.Status, file.LeaseExpiresAt });
            entity.HasIndex(file => new { file.JobId, file.Sequence }).IsUnique();
        });

        modelBuilder.Entity<PostflopBettingSpot>(entity =>
        {
            entity.HasKey(spot => new { spot.HandId, spot.Street });
            entity.Property(spot => spot.HandId).HasMaxLength(32);
            entity.Property(spot => spot.HeroPlayerId).HasMaxLength(255).IsRequired();
            entity.Property(spot => spot.PfrPlayerId).HasMaxLength(255).IsRequired();
            entity.Property(spot => spot.DefendingPlayerId).HasMaxLength(255).IsRequired();
            entity.Property(spot => spot.Street).HasConversion<string>().HasMaxLength(16);
            entity.Property(spot => spot.FlopHighCard).HasConversion<string>().HasMaxLength(8);
            entity.Property(spot => spot.FlopTexture).HasConversion<string>().HasMaxLength(16);
            entity
                .Property(spot => spot.FlopActionSequence)
                .HasConversion<string>()
                .HasMaxLength(8);
            entity.Property(spot => spot.FlopRankTexture).HasConversion<string>().HasMaxLength(16);
            entity
                .Property(spot => spot.TurnActionSequence)
                .HasConversion<string>()
                .HasMaxLength(8);
            entity.Property(spot => spot.TurnRunout).HasConversion<string>().HasMaxLength(24);
            entity.Property(spot => spot.RiverRunout).HasConversion<string>().HasMaxLength(24);
            entity
                .Property(spot => spot.RiverShowdownOutcome)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.VillainRiverBetShowdownOutcome)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.VillainRiverRaiseShowdownOutcome)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.HeroResponseToVillainRiverBet)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.HeroResponseToVillainRiverRaise)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.VillainResponseToHeroRiverBet)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity
                .Property(spot => spot.RiverBetResponseLine)
                .HasConversion<string>()
                .HasMaxLength(8);
            entity.Property(spot => spot.PfrPosition).HasConversion<string>().HasMaxLength(8);
            entity.Property(spot => spot.DefendingPosition).HasConversion<string>().HasMaxLength(8);
            entity.Property(spot => spot.ResponseTo).HasConversion<string>().HasMaxLength(16);
            entity.Property(spot => spot.ResponseAction).HasConversion<string>().HasMaxLength(16);
            entity.Property(spot => spot.PfrBetBb).HasPrecision(12, 4);
            entity.Property(spot => spot.DonkBetBb).HasPrecision(12, 4);
            entity.Property(spot => spot.ResponseAmountBb).HasPrecision(12, 4);
            entity.Property(spot => spot.HeroRiverBetToPotRatio).HasPrecision(12, 6);
            entity.Property(spot => spot.RiverBetToPotRatio).HasPrecision(12, 6);
            entity.HasIndex(spot => new
            {
                spot.Street,
                spot.PreflopRaiseCount,
                spot.FlopWentCheckCheck,
            });
            entity.HasIndex(spot => new { spot.Street, spot.FlopHighCard });
            entity.HasIndex(spot => new { spot.Street, spot.FlopTexture });
            entity.HasIndex(spot => new { spot.Street, spot.FlopActionSequence });
            entity.HasIndex(spot => new { spot.Street, spot.FlopRankTexture });
            entity.HasIndex(spot => new { spot.Street, spot.TurnActionSequence });
            entity.HasIndex(spot => new { spot.Street, spot.TurnRunout });
            entity.HasIndex(spot => new { spot.Street, spot.RiverRunout });
            entity
                .HasOne<Domain.PokerHand.PokerHand>()
                .WithMany()
                .HasForeignKey(spot => spot.HandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MassDataReprocessingJob>(entity =>
        {
            entity.HasKey(job => job.JobId);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(job => job.LastProcessedHandId).HasMaxLength(32);
            entity.Property(job => job.ErrorMessage).HasColumnType("longtext");
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
        });
    }
}
