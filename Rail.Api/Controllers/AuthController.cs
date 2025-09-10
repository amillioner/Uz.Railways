// Controllers/AuthController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rail.Authentication;
using Rail.Data.Models;

namespace Rail.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and get JWT token
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            if (result == null)
                return Unauthorized(new { error = "Invalid username or password" });

            return Ok(result);
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _authService.GetUserByUsernameAsync(username);
            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                roles = user.GetRoles(),
                createdAt = user.CreatedAt
            });
        }
    }
}


//// Controllers/HealthController.cs (Updated with RabbitMQ checks)
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using RailApi.Data;
//using RailApi.Services;
//using System.Diagnostics;

//namespace RailApi.Controllers;

//// Updated Controllers/TrainsController.cs (with authorization)
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using RailApi.Constants;
//using RailApi.Models;
//using RailApi.Services;

//namespace RailApi.Controllers;

//[ApiController]
//[Route("api/v1/trains")]
//[Authorize] // Require authentication for all endpoints
//public class TrainsController : ControllerBase
//{
//    private readonly ITrainService _trainService;
//    private readonly ILogger<TrainsController> _logger;

//    public TrainsController(ITrainService trainService, ILogger<TrainsController> logger)
//    {
//        _trainService = trainService;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Get paginated list of trains with filtering options
//    /// </summary>
//    [HttpGet]
//    [Authorize(Roles = $"{Roles.Reader},{Roles.Uploader},{Roles.Admin}")]
//    [ProducesResponseType(typeof(PagedResult<TrainDto>), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest)]
//    public async Task<ActionResult<PagedResult<TrainDto>>> GetTrains([FromQuery] TrainFilterDto filter)
//    {
//        if (!ModelState.IsValid)
//            return BadRequest(ModelState);

//        try
//        {
//            var result = await _trainService.GetTrainsAsync(filter);
//            return Ok(result);
//        }
//        catch (ArgumentException ex)
//        {
//            return BadRequest(new { error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Get detailed statistics for a specific train
//    /// </summary>
//    [HttpGet("{normalizedIndex}/stats")]
//    [Authorize(Roles = $"{Roles.Reader},{Roles.Uploader},{Roles.Admin}")]
//    [ProducesResponseType(typeof(TrainStatsDto), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status404NotFound)]
//    public async Task<ActionResult<TrainStatsDto>> GetTrainStats(string normalizedIndex)
//    {
//        try
//        {
//            var stats = await _trainService.GetTrainStatsAsync(normalizedIndex);
//            if (stats == null)
//                return NotFound(new { error = "Train not found" });

//            return Ok(stats);
//        }
//        catch (ArgumentException ex)
//        {
//            return BadRequest(new { error = ex.Message });
//        }
//    }
//}