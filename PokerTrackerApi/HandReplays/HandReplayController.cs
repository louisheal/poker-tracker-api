using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandReplays;

[ApiController]
[Route("api/handreplay")]
public class HandReplayController : ControllerBase
{
    private readonly IHandReplayRepository _repository;
    private readonly IHandReplayMapper _mapper;

    public HandReplayController(IHandReplayRepository repository, IHandReplayMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet("{handId}")]
    public async Task<ActionResult<HandReplayDto>> GetHandReplay(
        string handId,
        CancellationToken cancellationToken
    )
    {
        var replay = await _repository.GetHandReplayAsync(handId, cancellationToken);
        if (replay is null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map(replay));
    }
}
