using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandReprocessing;

[ApiController]
[Route("api/handreprocessing")]
public class HandReprocessingController : ControllerBase
{
    private readonly IHandReprocessingService _reprocessingService;

    public HandReprocessingController(IHandReprocessingService reprocessingService)
    {
        _reprocessingService = reprocessingService;
    }

    [HttpPost]
    public async Task ReprocessRawHands(CancellationToken cancellationToken)
    {
        await _reprocessingService.ReprocessRawHands(cancellationToken);
    }
}