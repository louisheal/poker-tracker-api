using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.Winrate;

[ApiController]
[Route("api/winrate")]
public class WinrateController : ControllerBase
{
    private readonly IWinrateRepository _repository;

    public WinrateController(IWinrateRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("graph")]
    public async Task<ActionResult<WinrateGraphDto>> GetWinrateGraph(
        CancellationToken cancellationToken
    )
    {
        var graph = await _repository.GetWinrateGraphAsync(cancellationToken);
        return Ok(graph);
    }
}
