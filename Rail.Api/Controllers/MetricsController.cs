using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rail.Authentication;

namespace Rail.Api.Controllers
{
    [ApiController]
    [Route("api/v1/metrics")]
    [Authorize(Roles = "admin")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        /// <summary>
        /// Get application metrics
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Dictionary<string, object>>> GetMetrics()
        {
            var metrics = await _metricsService.GetMetricsAsync();
            return Ok(metrics);
        }
    }
}