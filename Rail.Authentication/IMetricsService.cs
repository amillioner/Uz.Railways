using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rail.Authentication;

public interface IMetricsService
{
    void IncrementMessageProcessed(string status);
    void RecordMessageProcessingTime(double milliseconds);
    void IncrementApiRequest(string endpoint, string method, int statusCode);
    void RecordApiResponseTime(string endpoint, string method, double milliseconds);
    Task<Dictionary<string, object>> GetMetricsAsync();
}