using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandImporting;

[ApiController]
[Route("api/imports")]
public class HandImportController : ControllerBase
{
    private readonly IHandImportService _importService;

    public HandImportController(IHandImportService importService)
    {
        _importService = importService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<HandImportSummary>> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        if (files.Count == 0)
        {
            return BadRequest("At least one file is required.");
        }

        var summary = await _importService.ImportAsync(files, cancellationToken);
        return Ok(summary);
    }
}
