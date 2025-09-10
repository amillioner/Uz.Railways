using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rail.Indexing
{
    /// <summary>
    /// Класс для работы с индексом поезда
    /// </summary>
    public class TrainIndex
    {
        /// <summary>
        /// Код станции формирования (4 цифры)
        /// </summary>
        public string FormationStationCode { get; private set; }

        /// <summary>
        /// Номер состава (3 цифры)
        /// </summary>
        public string TrainNumber { get; private set; }

        /// <summary>
        /// Код станции назначения (4 цифры)
        /// </summary>
        public string DestinationStationCode { get; private set; }

        /// <summary>
        /// Нормализованный индекс в формате XXXX YYY ZZZZ
        /// </summary>
        public string NormalizedIndex => $"{FormationStationCode} {TrainNumber} {DestinationStationCode}";

        private TrainIndex(string formationStationCode, string trainNumber, string destinationStationCode)
        {
            FormationStationCode = formationStationCode;
            TrainNumber = trainNumber;
            DestinationStationCode = destinationStationCode;
        }

        /// <summary>
        /// Создает экземпляр TrainIndex из строкового представления индекса
        /// </summary>
        /// <param name="rawIndex">Сырой индекс поезда</param>
        /// <returns>Экземпляр TrainIndex</returns>
        /// <exception cref="TrainIndexValidationException">Если индекс невалидный</exception>
        public static TrainIndex Parse(string rawIndex)
        {
            if (string.IsNullOrWhiteSpace(rawIndex))
                throw new TrainIndexValidationException("Индекс не может быть пустым или содержать только пробелы");

            // Убираем все разделители и оставляем только цифры
            var digitsOnly = Regex.Replace(rawIndex, @"[^\d]", "");

            if (string.IsNullOrEmpty(digitsOnly))
                throw new TrainIndexValidationException("Индекс должен содержать цифры");

            // Разделяем на части по числовым группам
            var parts = ExtractNumericParts(rawIndex);

            if (parts.Length < 2)
                throw new TrainIndexValidationException("Индекс должен содержать минимум 2 числовые части");

            if (parts.Length > 3)
                throw new TrainIndexValidationException("Индекс должен содержать максимум 3 числовые части");

            return parts.Length == 2 ? ParseTwoParts(parts) : ParseThreeParts(parts);
        }

        /// <summary>
        /// Попытка создать экземпляр TrainIndex из строкового представления
        /// </summary>
        /// <param name="rawIndex">Сырой индекс поезда</param>
        /// <param name="trainIndex">Результирующий экземпляр TrainIndex</param>
        /// <returns>true, если парсинг успешен; иначе false</returns>
        public static bool TryParse(string rawIndex, out TrainIndex trainIndex)
        {
            try
            {
                trainIndex = Parse(rawIndex);
                return true;
            }
            catch (TrainIndexValidationException)
            {
                trainIndex = null;
                return false;
            }
        }

        /// <summary>
        /// Normalizes train index
        /// </summary>
        /// <param name="rawIndex">Raw index</param>
        /// <returns>Normalized index in format XXXX YYY ZZZZ</returns>
        public static string Normalize(string rawIndex)
        {
            return Parse(rawIndex).NormalizedIndex;
        }

        /// <summary>
        /// Attempts to normalize train index
        /// </summary>
        /// <param name="rawIndex">Raw index</param>
        /// <param name="normalizedIndex">Normalized index</param>
        /// <returns>true if normalization succeeds; otherwise false</returns>
        public static bool TryNormalize(string rawIndex, out string normalizedIndex)
        {
            if (TryParse(rawIndex, out var trainIndex))
            {
                normalizedIndex = trainIndex.NormalizedIndex;
                return true;
            }

            normalizedIndex = null;
            return false;
        }

        private static string[] ExtractNumericParts(string input)
        {
            var matches = Regex.Matches(input, @"\d+");
            return matches.Cast<Match>().Select(m => m.Value).ToArray();
        }

        private static TrainIndex ParseTwoParts(string[] parts)
        {
            var part1 = parts[0];
            var part2 = parts[1];

            // Первая часть всегда станция формирования
            var formationStation = NormalizeStationCode(part1);

            // Вторая часть может быть либо номером состава, либо станцией назначения
            if (IsTrainNumber(part2))
            {
                // Если вторая часть похожа на номер состава (2-3 цифры)
                var trainNumber = NormalizeTrainNumber(part2);
                throw new TrainIndexValidationException("Недостаточно данных: отсутствует код станции назначения");
            }
            else
            {
                // Вторая часть - станция назначения, номер состава неизвестен
                throw new TrainIndexValidationException("Недостаточно данных: отсутствует номер состава");
            }
        }

        private static TrainIndex ParseThreeParts(string[] parts)
        {
            var part1 = parts[0];
            var part2 = parts[1];
            var part3 = parts[2];

            // Первая часть всегда станция формирования
            var formationStation = NormalizeStationCode(part1);

            // Определяем, какая из частей 2 и 3 - номер состава, а какая - станция назначения
            string trainNumber;
            string destinationStation;

            if (IsTrainNumber(part2) && !IsTrainNumber(part3))
            {
                // part2 - номер состава, part3 - станция назначения
                trainNumber = NormalizeTrainNumber(part2);
                destinationStation = NormalizeStationCode(part3);
            }
            else if (!IsTrainNumber(part2) && IsTrainNumber(part3))
            {
                // part2 - станция назначения, part3 - номер состава
                destinationStation = NormalizeStationCode(part2);
                trainNumber = NormalizeTrainNumber(part3);
            }
            else if (IsTrainNumber(part2) && IsTrainNumber(part3))
            {
                // Обе части могут быть номером состава - выбираем более короткую
                if (part2.Length <= part3.Length && part2.Length <= 3)
                {
                    trainNumber = NormalizeTrainNumber(part2);
                    destinationStation = NormalizeStationCode(part3);
                }
                else if (part3.Length <= 3)
                {
                    trainNumber = NormalizeTrainNumber(part3);
                    destinationStation = NormalizeStationCode(part2);
                }
                else
                {
                    throw new TrainIndexValidationException("Не удается определить номер состава и станцию назначения");
                }
            }
            else
            {
                // Ни одна часть не похожа на номер состава
                throw new TrainIndexValidationException("Не найден номер состава (должен содержать 2-3 цифры)");
            }

            return new TrainIndex(formationStation, trainNumber, destinationStation);
        }

        private static bool IsTrainNumber(string part)
        {
            return part.Length >= 2 && part.Length <= 3 && part.All(char.IsDigit);
        }

        private static string NormalizeStationCode(string stationCode)
        {
            if (string.IsNullOrEmpty(stationCode) || !stationCode.All(char.IsDigit))
                throw new TrainIndexValidationException($"Station code must contain only digits: {stationCode}");

            if (stationCode.Length < 4)
                throw new TrainIndexValidationException($"Station code must contain at least 4 digits: {stationCode}");

            if (stationCode.Length > 5)
                throw new TrainIndexValidationException($"Station code must contain at most 5 digits: {stationCode}");

            // If 5 digits - truncate the last one
            return stationCode.Length == 5 ? stationCode.Substring(0, 4) : stationCode;
        }

        private static string NormalizeTrainNumber(string trainNumber)
        {
            if (string.IsNullOrEmpty(trainNumber) || !trainNumber.All(char.IsDigit))
                throw new TrainIndexValidationException($"Train number must contain only digits: {trainNumber}");

            if (trainNumber.Length < 2 || trainNumber.Length > 3)
                throw new TrainIndexValidationException($"Train number must contain 2-3 digits: {trainNumber}");

            // Pad to 3 digits with leading zero if necessary
            return trainNumber.PadLeft(3, '0');
        }

        public override string ToString()
        {
            return NormalizedIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is TrainIndex other)
            {
                return FormationStationCode == other.FormationStationCode &&
                       TrainNumber == other.TrainNumber &&
                       DestinationStationCode == other.DestinationStationCode;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FormationStationCode, TrainNumber, DestinationStationCode);
        }
    }
}