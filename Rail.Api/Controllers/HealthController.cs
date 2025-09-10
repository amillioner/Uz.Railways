using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rail.Data.Data;
using Rail.RabbitMQ.Services;

namespace Rail.Api.Controllers;

[ApiController]
[Route("health")]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly RailDbContext _context;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        RailDbContext context,
        IRabbitMqService rabbitMqService,
        ILogger<HealthController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        var healthChecks = new Dictionary<string, object>();
        var overallStatus = "Healthy";
        var timestamp = DateTime.UtcNow;

        // Database health check
        try
        {
            var dbCheckStart = Stopwatch.StartNew();
            await _context.Database.CanConnectAsync();
            dbCheckStart.Stop();

            healthChecks["database"] = new
            {
                status = "Healthy",
                responseTimeMs = dbCheckStart.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            overallStatus = "Unhealthy";
            healthChecks["database"] = new
            {
                status = "Unhealthy",
                error = ex.Message
            };
            _logger.LogError(ex, "Database health check failed");
        }

        // RabbitMQ health check
        try
        {
            var rabbitCheckStart = Stopwatch.StartNew();
            var connection = _rabbitMqService.GetConnection();
            var isOpen = connection.IsOpen;
            rabbitCheckStart.Stop();

            healthChecks["rabbitmq"] = new
            {
                status = isOpen ? "Healthy" : "Unhealthy",
                responseTimeMs = rabbitCheckStart.ElapsedMilliseconds,
                isConnected = isOpen
            };

            if (!isOpen)
                overallStatus = "Degraded";
        }
        catch (Exception ex)
        {
            overallStatus = "Degraded";
            healthChecks["rabbitmq"] = new
            {
                status = "Unhealthy",
                error = ex.Message
            };
            _logger.LogError(ex, "RabbitMQ health check failed");
        }

        // System health
        var process = Process.GetCurrentProcess();
        healthChecks["system"] = new
        {
            status = "Healthy",
            memoryUsageMB = process.WorkingSet64 / 1024 / 1024,
            cpuTimeMs = process.TotalProcessorTime.TotalMilliseconds,
            uptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds
        };

        var result = new
        {
            status = overallStatus,
            timestamp,
            checks = healthChecks
        };

        var statusCode = overallStatus switch
        {
            "Healthy" => 200,
            "Degraded" => 200, // Still operational
            _ => 503
        };

        return StatusCode(statusCode, result);
    }
}