using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rail.Authentication;

public class MetricsService : IMetricsService
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, List<double>> _timings = new();
    private readonly object _timingsLock = new();

    public void IncrementMessageProcessed(string status)
    {
        _counters.AddOrUpdate($"messages_processed_{status}", 1, (k, v) => v + 1);
        _counters.AddOrUpdate("messages_processed_total", 1, (k, v) => v + 1);
    }

    public void RecordMessageProcessingTime(double milliseconds)
    {
        RecordTiming("message_processing_time", milliseconds);
    }

    public void IncrementApiRequest(string endpoint, string method, int statusCode)
    {
        _counters.AddOrUpdate($"api_requests_total", 1, (k, v) => v + 1);
        _counters.AddOrUpdate($"api_requests_{method}_{endpoint}", 1, (k, v) => v + 1);
        _counters.AddOrUpdate($"api_responses_{statusCode}", 1, (k, v) => v + 1);
    }

    public void RecordApiResponseTime(string endpoint, string method, double milliseconds)
    {
        RecordTiming($"api_response_time_{method}_{endpoint}", milliseconds);
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();

        // Add counters
        foreach (var counter in _counters)
        {
            metrics[counter.Key] = counter.Value;
        }

        // Add timing statistics
        lock (_timingsLock)
        {
            foreach (var timing in _timings)
            {
                if (timing.Value.Count > 0)
                {
                    metrics[$"{timing.Key}_count"] = timing.Value.Count;
                    metrics[$"{timing.Key}_avg"] = timing.Value.Average();
                    metrics[$"{timing.Key}_min"] = timing.Value.Min();
                    metrics[$"{timing.Key}_max"] = timing.Value.Max();
                    metrics[$"{timing.Key}_p95"] = Percentile(timing.Value, 0.95);
                }
            }
        }

        // Add system metrics
        var process = Process.GetCurrentProcess();
        metrics["memory_working_set_mb"] = process.WorkingSet64 / 1024 / 1024;
        metrics["cpu_time_ms"] = process.TotalProcessorTime.TotalMilliseconds;
        metrics["uptime_seconds"] = (DateTime.UtcNow - process.StartTime).TotalSeconds;

        return await Task.FromResult(metrics);
    }

    private void RecordTiming(string name, double milliseconds)
    {
        lock (_timingsLock)
        {
            if (!_timings.ContainsKey(name))
                _timings[name] = new List<double>();

            _timings[name].Add(milliseconds);

            // Keep only last 1000 measurements per metric
            if (_timings[name].Count > 1000)
                _timings[name] = _timings[name].TakeLast(1000).ToList();
        }
    }

    private static double Percentile(List<double> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}