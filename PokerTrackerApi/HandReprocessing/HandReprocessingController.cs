using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandReprocessing;

[ApiController]
[Route("api/handreprocessing")]
public class HandReprocessingController : ControllerBase
{

    public HandReprocessingController()
    {

    }

    [HttpPost]
    public async Task ReprocessRawHands()
    {

    }
}