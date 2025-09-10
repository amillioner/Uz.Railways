using System.Collections.Generic;
using System.Linq;

namespace RailCli;

public class CsvStatistics
{
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int EmptyRecords { get; set; }

    public Dictionary<string, int> FormationStations { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> DestinationStations { get; } = new Dictionary<string, int>();
    public Dictionary<string, string> NormalizationExamples { get; } = new Dictionary<string, string>();
    public List<string> InvalidIndexes { get; } = new List<string>();

    public void AddNormalizedIndex(string normalizedIndex, string originalIndex)
    {
        var parts = normalizedIndex.Split(' ');
        if (parts.Length == 3)
        {
            var formationStation = parts[0];
            var destinationStation = parts[2];

            FormationStations[formationStation] = FormationStations.GetValueOrDefault(formationStation, 0) + 1;
            DestinationStations[destinationStation] = DestinationStations.GetValueOrDefault(destinationStation, 0) + 1;

            // Сохраняем только уникальные примеры нормализации
            if (!NormalizationExamples.ContainsKey(originalIndex))
            {
                NormalizationExamples[originalIndex] = normalizedIndex;
            }
        }
    }

    public IEnumerable<KeyValuePair<string, int>> GetTopFormationStations(int count)
    {
        return FormationStations
            .OrderByDescending(x => x.Value)
            .Take(count);
    }

    public IEnumerable<KeyValuePair<string, int>> GetTopDestinationStations(int count)
    {
        return DestinationStations
            .OrderByDescending(x => x.Value)
            .Take(count);
    }

    public IEnumerable<KeyValuePair<string, string>> GetNormalizationExamples(int count)
    {
        return NormalizationExamples.Take(count);
    }
}