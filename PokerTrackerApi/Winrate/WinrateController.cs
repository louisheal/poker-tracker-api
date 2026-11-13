using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.Winrate;

[ApiController]
[Route("api/winrate")]
public class WinrateController(IWinrateService winrateService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Reprocess(CancellationToken cancellationToken)
    {
        return Ok(await winrateService.GetWinrateDto(cancellationToken));
    }
}
