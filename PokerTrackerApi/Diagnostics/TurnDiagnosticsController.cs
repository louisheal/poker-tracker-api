using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.Diagnostics;

[ApiController]
[Route("api/diagnostics")]
public class TurnDiagnosticsController : ControllerBase
{
    private readonly ITurnDiagnosticsRepository _repository;

    public TurnDiagnosticsController(ITurnDiagnosticsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("turn")]
    public async Task<ActionResult<TurnDiagnosticsDto>> GetTurnDiagnostics(
        [FromQuery] IReadOnlyList<TurnPotType>? potTypes,
        CancellationToken cancellationToken
    )
    {
        var diagnostics = await _repository.GetTurnDiagnosticsAsync(potTypes, cancellationToken);
        return Ok(diagnostics);
    }
}
