using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rail.Data.Models;
using Rail.Services.Services;
using System.ComponentModel.DataAnnotations;

namespace Rail.Api.Controllers
{
    [ApiController]
    [Route("api/v1/trains")]
    [Produces("application/json")]
    public class TrainsController : ControllerBase
    {
        private readonly ITrainService _trainService;
        private readonly ICsvProcessingService _csvProcessingService;
        private readonly IValidator<TrainFilterDto> _filterValidator;
        private readonly IValidator<IFormFile> _fileValidator;
        private readonly ILogger<TrainsController> _logger;

        public TrainsController(
            ITrainService trainService,
            ICsvProcessingService csvProcessingService,
            IValidator<TrainFilterDto> filterValidator,
            IValidator<IFormFile> fileValidator,
            ILogger<TrainsController> logger)
        {
            _trainService = trainService;
            _csvProcessingService = csvProcessingService;
            _filterValidator = filterValidator;
            _fileValidator = fileValidator;
            _logger = logger;
        }

        /// <summary>
        /// Upload CSV file for background processing
        /// </summary>
        /// <param name="file">CSV file containing train and wagon data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload job information</returns>
        [HttpPost("upload/csv")]
        [ProducesResponseType(typeof(JobStatusDto), 202)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 500)]
        public async Task<IActionResult> UploadCsv(
            [Required] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate file
                var validationResult = await _fileValidator.ValidateAsync(file, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ApiErrorResponse
                    {
                        Error = "Validation Failed",
                        Message = "File validation failed",
                        Details = errors,
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                _logger.LogInformation("Starting CSV upload processing for file: {FileName}", file.FileName);

                // Start background processing
                using var stream = file.OpenReadStream();
                var jobId = await _csvProcessingService.ProcessCsvAsync(stream, cancellationToken);

                var jobStatus = _csvProcessingService.GetJobStatus(jobId);

                return Accepted(new { JobId = jobId, Status = jobStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CSV file: {FileName}", file?.FileName);
                return StatusCode(500, new ApiErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = "An error occurred while processing the upload",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get upload job status
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status information</returns>
        [HttpGet("upload/status/{jobId}")]
        [ProducesResponseType(typeof(JobStatusDto), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public IActionResult GetUploadStatus(string jobId)
        {
            var status = _csvProcessingService.GetJobStatus(jobId);
            if (status == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    Error = "Job Not Found",
                    Message = $"Job with ID '{jobId}' was not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(status);
        }

        /// <summary>
        /// Get trains with filtering, sorting and pagination
        /// </summary>
        /// <param name="filter">Filter parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of trains</returns>
        [HttpGet("trains")]
        [ProducesResponseType(typeof(PagedResponse<TrainDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetTrains(
            [FromQuery] TrainFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate filter
                var validationResult = await _filterValidator.ValidateAsync(filter, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ApiErrorResponse
                    {
                        Error = "Validation Failed",
                        Message = "Filter validation failed",
                        Details = errors,
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                _logger.LogInformation("Getting trains with filter: {@Filter}", filter);

                var result = await _trainService.GetTrainsAsync(filter, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trains");
                return StatusCode(500, new ApiErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = "An error occurred while retrieving trains",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get detailed statistics for a specific train
        /// </summary>
        /// <param name="normalizedIndex">Normalized train index (format: XXXX YYY ZZZZ)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Train statistics including wagon details</returns>
        [HttpGet("trains/{normalizedIndex}/stats")]
        [ProducesResponseType(typeof(TrainStatsDto), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetTrainStats(
            string normalizedIndex,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(normalizedIndex))
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Error = "Invalid Parameter",
                        Message = "Normalized index is required",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                _logger.LogInformation("Getting stats for train: {Index}", normalizedIndex);

                var stats = await _trainService.GetTrainStatsAsync(normalizedIndex, cancellationToken);

                if (stats == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Error = "Train Not Found",
                        Message = $"Train with index '{normalizedIndex}' was not found",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting train stats for index: {Index}", normalizedIndex);
                return StatusCode(500, new ApiErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = "An error occurred while retrieving train statistics",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>API health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
    }
}