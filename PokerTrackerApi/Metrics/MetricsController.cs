using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.Metrics;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsRepository _repository;

    public MetricsController(IMetricsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<MetricsDto>> GetMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _repository.GetMetricsAsync(cancellationToken);
        return Ok(metrics);
    }
}
