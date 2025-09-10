namespace Rail.Data.Models;

public class MessageProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
    public int? TrainId { get; set; }
    public int? WagonId { get; set; }

    public static MessageProcessingResult SuccessResult(int trainId, int wagonId)
        => new() { Success = true, TrainId = trainId, WagonId = wagonId };

    public static MessageProcessingResult FailureResult(string error, bool shouldRetry = true)
        => new() { Success = false, ErrorMessage = error, ShouldRetry = shouldRetry };

    public static MessageProcessingResult DuplicateResult()
        => new() { Success = true, ErrorMessage = "Event already processed" };
}