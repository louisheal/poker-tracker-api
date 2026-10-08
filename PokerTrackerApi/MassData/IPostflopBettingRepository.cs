using PokerTrackerApi.Domain;

namespace PokerTrackerApi.MassData;

public interface IPostflopBettingRepository
{
    Task<PostflopBettingResponseDto> GetPostflopBettingAsync(
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        CancellationToken cancellationToken
    );
}
