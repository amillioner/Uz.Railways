namespace Rail.Services.Services;

internal class CsvRecord
{
    public int LineNumber { get; set; }
    public string RawTrainIndex { get; set; } = string.Empty;
    public string WagonNumber { get; set; } = string.Empty;
    public string IsLoadedText { get; set; } = string.Empty;
    public string WeightText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
}