using Microsoft.AspNetCore.Mvc;

namespace PokerTrackerApi.HandImporting;

[ApiController]
[Route("api/imports")]
public class HandImportController : ControllerBase
{
    private readonly IHandImportService _service;

    public HandImportController(IHandImportService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        if (files.Count == 0)
        {
            return BadRequest("At least one file is required.");
        }

        // TODO : add try/catch ?
        await _service.ImportFiles(files, cancellationToken);
        return NoContent();
    }
}
