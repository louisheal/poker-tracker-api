using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandReplays;

[ApiController]
[Route("api/handreplay")]
public class HandReplayController : ControllerBase
{
    private readonly IHandReplayRepository _repository;

    public HandReplayController(IHandReplayRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{handId}")]
    public async Task<ActionResult<HandReplay>> GetHandReplay(
        string handId,
        CancellationToken cancellationToken
    )
    {
        var replayDto = await _repository.GetHandReplayAsync(handId, cancellationToken);
        if (replayDto is null)
        {
            return NotFound();
        }

        return Ok(replayDto);
    }
}
