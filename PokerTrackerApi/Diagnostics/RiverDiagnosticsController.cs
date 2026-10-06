using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.Diagnostics;

[ApiController]
[Route("api/diagnostics")]
public class RiverDiagnosticsController : ControllerBase
{
    private readonly IRiverDiagnosticsRepository _repository;

    public RiverDiagnosticsController(IRiverDiagnosticsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("river")]
    public async Task<ActionResult<RiverDiagnosticsDto>> GetRiverDiagnostics(
        CancellationToken cancellationToken
    )
    {
        var diagnostics = await _repository.GetRiverDiagnosticsAsync(cancellationToken);
        return Ok(diagnostics);
    }
}
