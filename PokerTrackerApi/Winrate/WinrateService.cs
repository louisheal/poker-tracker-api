using PokerTrackerApi.Winrate.Repository;

namespace PokerTrackerApi.Winrate;

public interface IWinrateService
{
    Task<WinrateDto> GetWinrateDto(CancellationToken cancellationToken);
}

public class WinrateService(IWinrateRepository winrateRepository) : IWinrateService
{
    private const int BatchSize = 100;

    public async Task<WinrateDto> GetWinrateDto(CancellationToken cancellationToken)
    {
        var points = new List<WinrateDataPoint>();
        var handCount = 0;
        decimal netWonBB = 0;
        decimal wonWithShowdownBB = 0;
        decimal wonWithoutShowdownBB = 0;
        decimal netWonBeforeRakeBB = 0;
        decimal wonWithShowdownBeforeRakeBB = 0;
        decimal wonWithoutShowdownBeforeRakeBB = 0;

        await foreach (
            var winrate in winrateRepository
                .GetWinrates(cancellationToken)
                .WithCancellation(cancellationToken)
        )
        {
            handCount++;
            netWonBB += winrate.NetWonBB;
            wonWithShowdownBB += winrate.WonWithShowdownBB;
            wonWithoutShowdownBB += winrate.WonWithoutShowdownBB;
            netWonBeforeRakeBB += winrate.NetWonBeforeRakeBB;
            wonWithShowdownBeforeRakeBB += winrate.WonWithShowdownBeforeRakeBB;
            wonWithoutShowdownBeforeRakeBB += winrate.WonWithoutShowdownBeforeRakeBB;

            if (handCount % BatchSize == 0)
            {
                points.Add(
                    CreateDataPoint(
                        handCount,
                        netWonBB,
                        wonWithShowdownBB,
                        wonWithoutShowdownBB,
                        netWonBeforeRakeBB,
                        wonWithShowdownBeforeRakeBB,
                        wonWithoutShowdownBeforeRakeBB
                    )
                );
            }
        }

        if (handCount % BatchSize != 0)
        {
            points.Add(
                CreateDataPoint(
                    handCount,
                    netWonBB,
                    wonWithShowdownBB,
                    wonWithoutShowdownBB,
                    netWonBeforeRakeBB,
                    wonWithShowdownBeforeRakeBB,
                    wonWithoutShowdownBeforeRakeBB
                )
            );
        }

        return new WinrateDto(points);
    }

    private static WinrateDataPoint CreateDataPoint(
        int handCount,
        decimal netWonBB,
        decimal wonWithShowdownBB,
        decimal wonWithoutShowdownBB,
        decimal netWonBeforeRakeBB,
        decimal wonWithShowdownBeforeRakeBB,
        decimal wonWithoutShowdownBeforeRakeBB
    ) =>
        new(
            handCount,
            netWonBB,
            wonWithShowdownBB,
            wonWithoutShowdownBB,
            netWonBeforeRakeBB,
            wonWithShowdownBeforeRakeBB,
            wonWithoutShowdownBeforeRakeBB
        );
}
