using Microsoft.AspNetCore.Mvc;
using PokerTrackerApi.HandImporting.Jobs;

namespace PokerTrackerApi.HandImporting;

[ApiController]
[Route("api/imports")]
public class HandImportController : ControllerBase
{
    private readonly IHandImportJobService _jobService;

    public HandImportController(IHandImportJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<HandImportJobDto>> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        if (files.Count == 0)
        {
            return BadRequest("At least one file is required.");
        }

        var job = await _jobService.EnqueueAsync(files, cancellationToken);
        return AcceptedAtAction(nameof(GetJob), new { jobId = job.JobId }, job);
    }

    [HttpGet("active")]
    public async Task<ActionResult<HandImportJobDto[]>> GetActiveJobs(
        CancellationToken cancellationToken
    ) => Ok(await _jobService.GetActiveAsync(cancellationToken));

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<HandImportJobDto>> GetJob(
        Guid jobId,
        CancellationToken cancellationToken
    )
    {
        var job = await _jobService.GetAsync(jobId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }
}
