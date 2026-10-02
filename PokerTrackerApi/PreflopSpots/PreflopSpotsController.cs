using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.PreflopSpots;

[ApiController]
[Route("api/preflopspots")]
public class PreflopSpotsController : ControllerBase
{
    private readonly IPreflopSpotRepository _repository;

    public PreflopSpotsController(IPreflopSpotRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("range")]
    public async Task<ActionResult<RangeActionsDto>> GetRange(
        [FromQuery] string? spotKey,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(spotKey) || spotKey.Length > 256)
        {
            return BadRequest("A valid spotKey is required.");
        }

        var range = await _repository.GetRangeAsync(spotKey, cancellationToken);
        return Ok(range);
    }
}
