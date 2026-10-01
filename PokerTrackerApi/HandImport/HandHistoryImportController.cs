using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandImport;

[ApiController]
[Route("api/imports")]
public class HandHistoryImportController : ControllerBase
{
    private readonly IHandImportervice _importService;

    public HandHistoryImportController(IHandImportervice importService)
    {
        _importService = importService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<HandImportSummary>> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return BadRequest("At least one file is required.");
        }

        var summary = await _importService.ImportAsync(files, cancellationToken);
        return Ok(summary);
    }
}