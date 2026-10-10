using PokerTrackerApi.Domain;

namespace PokerTrackerApi.MassData;

public interface IPostflopBettingRepository
{
    Task<PostflopBetResponseBucketsDto> GetFlopResponseBucketsAsync(
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        CancellationToken cancellationToken
    );

    Task<PostflopBetResponseBucketsDto> GetTurnResponseBucketsAsync(
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        IReadOnlyCollection<PostflopActionSequence>? turnActionSequences,
        IReadOnlyCollection<PostflopRunout>? turnRunouts,
        CancellationToken cancellationToken
    );

    Task<PostflopBettingResponseDto> GetPostflopBettingAsync(
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        IReadOnlyCollection<PostflopActionSequence>? turnActionSequences,
        IReadOnlyCollection<PostflopRunout>? turnRunouts,
        IReadOnlyCollection<PostflopRunout>? riverRunouts,
        PostflopRiverBetSizeCategory? riverBetSizeCategory,
        decimal? minRiverBetToPotPercent,
        decimal? maxRiverBetToPotPercent,
        CancellationToken cancellationToken
    );
}
