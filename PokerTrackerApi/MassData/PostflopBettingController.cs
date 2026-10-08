using Microsoft.AspNetCore.Mvc;
using PokerTrackerApi.Domain;

namespace PokerTrackerApi.MassData;

[ApiController]
[Route("api/massdata")]
public class PostflopBettingController : ControllerBase
{
    private readonly IPostflopBettingRepository _repository;
    private readonly IMassDataReprocessingService _reprocessingService;

    public PostflopBettingController(
        IPostflopBettingRepository repository,
        IMassDataReprocessingService reprocessingService
    )
    {
        _repository = repository;
        _reprocessingService = reprocessingService;
    }

    [HttpGet("postflop-betting")]
    public async Task<ActionResult<PostflopBettingResponseDto>> GetPostflopBetting(
        [FromQuery] bool? pfrInPosition,
        [FromQuery] PokerPosition? ipPosition,
        [FromQuery] PokerPosition? oopPosition,
        [FromQuery] Rank? flopHighCard,
        [FromQuery] List<FlopTexture>? flopTextures,
        [FromQuery] List<PostflopPotType>? potTypes,
        [FromQuery] List<PostflopActionSequence>? flopActionSequences,
        [FromQuery] List<FlopRankTexture>? flopRankTextures,
        [FromQuery] List<PostflopActionSequence>? turnActionSequences,
        [FromQuery] List<PostflopRunout>? turnRunouts,
        [FromQuery] List<PostflopRunout>? riverRunouts,
        [FromQuery] PostflopRiverBetSizeCategory? riverBetSizeCategory,
        [FromQuery] decimal? minRiverBetToPotPercent,
        [FromQuery] decimal? maxRiverBetToPotPercent,
        [FromQuery(Name = "heroRiverBetSizeCategory")]
            PostflopRiverBetSizeCategory? legacyHeroRiverBetSizeCategory,
        [FromQuery(Name = "minHeroRiverBetToPotPercent")]
            decimal? legacyMinHeroRiverBetToPotPercent,
        [FromQuery(Name = "maxHeroRiverBetToPotPercent")]
            decimal? legacyMaxHeroRiverBetToPotPercent,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await _repository.GetPostflopBettingAsync(
                pfrInPosition,
                ipPosition,
                oopPosition,
                flopHighCard,
                flopTextures,
                potTypes,
                flopActionSequences,
                flopRankTextures,
                turnActionSequences,
                turnRunouts,
                riverRunouts,
                riverBetSizeCategory ?? legacyHeroRiverBetSizeCategory,
                minRiverBetToPotPercent ?? legacyMinHeroRiverBetToPotPercent,
                maxRiverBetToPotPercent ?? legacyMaxHeroRiverBetToPotPercent,
                cancellationToken
            )
        );

    [HttpPost("reprocess")]
    public async Task<ActionResult<MassDataReprocessingJobDto>> Reprocess(
        CancellationToken cancellationToken
    )
    {
        var job = await _reprocessingService.EnqueueAsync(cancellationToken);
        return AcceptedAtAction(nameof(GetReprocessingJob), new { jobId = job.JobId }, job);
    }

    [HttpGet("reprocess/{jobId:guid}")]
    public async Task<ActionResult<MassDataReprocessingJobDto>> GetReprocessingJob(
        Guid jobId,
        CancellationToken cancellationToken
    )
    {
        var job = await _reprocessingService.GetJobAsync(jobId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }
}
