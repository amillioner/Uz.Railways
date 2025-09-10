using Rail.Indexing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RailCli
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    ShowHelp();
                    return 0;
                }

                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "normalize":
                        return HandleNormalizeCommand(args);

                    case "stats":
                        return HandleStatsCommand(args);

                    case "--help":
                    case "-h":
                    case "help":
                        ShowHelp();
                        return 0;

                    default:
                        Console.WriteLine($"Неизвестная команда: {command}");
                        ShowHelp();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return 1;
            }
        }

        private static int HandleNormalizeCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Ошибка: не указан индекс для нормализации");
                Console.WriteLine("Использование: rail-cli normalize <rawIndex>");
                return 1;
            }

            var rawIndex = args[1];

            try
            {
                var normalizedIndex = TrainIndex.Normalize(rawIndex);
                Console.WriteLine(normalizedIndex);
                return 0;
            }
            catch (TrainIndexValidationException ex)
            {
                Console.WriteLine($"Ошибка валидации индекса: {ex.Message}");
                return 1;
            }
        }

        private static int HandleStatsCommand(string[] args)
        {
            string fileName = null;

            // Парсим аргументы
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--file" && i + 1 < args.Length)
                {
                    fileName = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Ошибка: не указан файл");
                Console.WriteLine("Использование: rail-cli stats --file <filename.csv>");
                return 1;
            }

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Ошибка: файл '{fileName}' не найден");
                return 1;
            }

            try
            {
                var statistics = ProcessCsvFile(fileName);
                DisplayStatistics(statistics);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки файла: {ex.Message}");
                return 1;
            }
        }

        private static CsvStatistics ProcessCsvFile(string fileName)
        {
            var lines = File.ReadAllLines(fileName);
            var statistics = new CsvStatistics();

            if (lines.Length == 0)
            {
                return statistics;
            }

            // Пропускаем заголовок, если он есть
            var dataLines = lines.Skip(1);

            foreach (var line in dataLines)
            {
                statistics.TotalRecords++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    statistics.EmptyRecords++;
                    continue;
                }

                // Предполагаем, что индекс находится в первой колонке CSV
                var columns = ParseCsvLine(line);
                if (columns.Length == 0)
                {
                    statistics.EmptyRecords++;
                    continue;
                }

                var rawIndex = columns[0].Trim();
                if (string.IsNullOrWhiteSpace(rawIndex))
                {
                    statistics.EmptyRecords++;
                    continue;
                }

                if (TrainIndex.TryNormalize(rawIndex, out var normalizedIndex))
                {
                    statistics.ValidRecords++;
                    statistics.AddNormalizedIndex(normalizedIndex, rawIndex);
                }
                else
                {
                    statistics.InvalidRecords++;
                    statistics.InvalidIndexes.Add(rawIndex);
                }
            }

            return statistics;
        }

        private static string[] ParseCsvLine(string line)
        {
            var columns = new List<string>();
            var currentColumn = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    columns.Add(currentColumn);
                    currentColumn = "";
                }
                else
                {
                    currentColumn += c;
                }
            }

            columns.Add(currentColumn);
            return columns.ToArray();
        }

        private static void DisplayStatistics(CsvStatistics stats)
        {
            Console.WriteLine("=== СТАТИСТИКА ОБРАБОТКИ CSV ===");
            Console.WriteLine();

            Console.WriteLine($"Всего записей: {stats.TotalRecords}");
            Console.WriteLine($"Валидных индексов: {stats.ValidRecords}");
            Console.WriteLine($"Невалидных индексов: {stats.InvalidRecords}");
            Console.WriteLine($"Пустых записей: {stats.EmptyRecords}");
            Console.WriteLine();

            if (stats.ValidRecords > 0)
            {
                Console.WriteLine("=== ТОП СТАНЦИЙ ФОРМИРОВАНИЯ ===");
                var topFormationStations = stats.GetTopFormationStations(5);
                foreach (var station in topFormationStations)
                {
                    Console.WriteLine($"{station.Key}: {station.Value} поездов");
                }

                Console.WriteLine();

                Console.WriteLine("=== ТОП СТАНЦИЙ НАЗНАЧЕНИЯ ===");
                var topDestinationStations = stats.GetTopDestinationStations(5);
                foreach (var station in topDestinationStations)
                {
                    Console.WriteLine($"{station.Key}: {station.Value} поездов");
                }

                Console.WriteLine();

                Console.WriteLine("=== ПРИМЕРЫ НОРМАЛИЗОВАННЫХ ИНДЕКСОВ ===");
                var examples = stats.GetNormalizationExamples(5);
                foreach (var example in examples)
                {
                    Console.WriteLine($"{example.Key} → {example.Value}");
                }
            }

            if (stats.InvalidRecords > 0)
            {
                Console.WriteLine();
                Console.WriteLine("=== ПРИМЕРЫ НЕВАЛИДНЫХ ИНДЕКСОВ ===");
                var invalidExamples = stats.InvalidIndexes.Take(10);
                foreach (var invalid in invalidExamples)
                {
                    Console.WriteLine($"'{invalid}'");
                }

                if (stats.InvalidIndexes.Count > 10)
                {
                    Console.WriteLine($"... и еще {stats.InvalidIndexes.Count - 10}");
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Rail CLI - Train Index Processing Utility");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rail-cli normalize <rawIndex>     Normalize train index");
            Console.WriteLine("  rail-cli stats --file <file.csv>  Show CSV file statistics");
            Console.WriteLine("  rail-cli help                     Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  rail-cli normalize \"7478-035-6980\"");
            Console.WriteLine("  rail-cli stats --file wagons.csv");
        }
    }
}