using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rail.Data.Constants;
using Rail.Services.Services;

namespace Rail.Api.Controllers;

[ApiController]
[Route("api/v1/upload")]
[Authorize(Roles = $"{Roles.Uploader},{Roles.Admin}")] // Only uploaders and admins can upload
public class UploadController : ControllerBase
{
    private readonly ICsvProcessingService _csvProcessingService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(ICsvProcessingService csvProcessingService, ILogger<UploadController> logger)
    {
        _csvProcessingService = csvProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Upload and process CSV file containing wagon data
    /// </summary>
    [HttpPost("csv")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UploadCsv(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required" });

        if (!file.ContentType.Contains("csv") && !file.ContentType.Contains("text"))
            return BadRequest(new { error = "Invalid file type. Only CSV files are allowed." });

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
            return BadRequest(new { error = "File size exceeds 10MB limit" });

        try
        {
            using var stream = file.OpenReadStream();
            var jobId = await _csvProcessingService.ProcessCsvAsync(stream, cancellationToken);

            _logger.LogInformation("CSV upload initiated by user {Username}: JobId={JobId}, FileName={FileName}, FileSize={FileSize}",
                User.Identity?.Name, jobId, file.FileName, file.Length);

            return Accepted(new { jobId, status = "Running" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV upload");
            return BadRequest(new { error = "Failed to process CSV file" });
        }
    }

    /// <summary>
    /// Get the status of a CSV processing job
    /// </summary>
    [HttpGet("status/{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetUploadStatus(string jobId)
    {
        var status = _csvProcessingService.GetJobStatus(jobId);

        if (status == null)
            return NotFound(new { error = "Job not found" });

        return Ok(status);
    }
}