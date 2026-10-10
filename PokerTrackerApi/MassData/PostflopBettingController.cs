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

    [HttpGet("postflop-betting/{street}/response-buckets")]
    public async Task<ActionResult<PostflopBetResponseBucketsDto>> GetResponseBuckets(
        [FromRoute] PokerStreet street,
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
        CancellationToken cancellationToken
    ) =>
        street == PokerStreet.Preflop
            ? BadRequest("Response buckets are available for Flop, Turn, and River.")
            : Ok(
                await _repository.GetResponseBucketsAsync(
                    street,
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
                    riverBetSizeCategory,
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
