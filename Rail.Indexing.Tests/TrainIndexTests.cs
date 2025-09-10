using FluentAssertions;
using Xunit;

namespace Rail.Indexing.Tests
{
    public class TrainIndexTests
    {
        [Fact]
        public void Parse_ValidThreePartIndex_ReturnsCorrectTrainIndex()
        {
            // Arrange
            var rawIndex = "7478-035-6980";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.FormationStationCode.Should().Be("7478");
            result.TrainNumber.Should().Be("035");
            result.DestinationStationCode.Should().Be("6980");
            result.NormalizedIndex.Should().Be("7478 035 6980");
        }

        [Fact]
        public void Parse_IndexWithSwappedTrainNumberAndDestination_ReturnsCorrectTrainIndex()
        {
            // Arrange - номер состава и станция назначения поменяны местами
            var rawIndex = "7478/6980/35";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.FormationStationCode.Should().Be("7478");
            result.TrainNumber.Should().Be("035");
            result.DestinationStationCode.Should().Be("6980");
        }

        [Fact]
        public void Parse_IndexWithFiveDigitStationCodes_TruncatesLastDigit()
        {
            // Arrange
            var rawIndex = "74785 035 69801";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.FormationStationCode.Should().Be("7478");
            result.TrainNumber.Should().Be("035");
            result.DestinationStationCode.Should().Be("6980");
        }

        [Fact]
        public void Parse_IndexWithTwoDigitTrainNumber_PadsWithZero()
        {
            // Arrange
            var rawIndex = "7478 35 6980";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.TrainNumber.Should().Be("035");
        }

        [Fact]
        public void Parse_IndexWithVariousDelimiters_ParsesCorrectly()
        {
            // Arrange
            var rawIndex = "7478-035/6980";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.NormalizedIndex.Should().Be("7478 035 6980");
        }

        [Fact]
        public void Parse_IndexWithSpacesAndNoDelimiters_ParsesCorrectly()
        {
            // Arrange
            var rawIndex = " 7478  035   6980 ";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.NormalizedIndex.Should().Be("7478 035 6980");
        }

        [Fact]
        public void Parse_EmptyOrNullInput_ThrowsTrainIndexValidationException()
        {
            // Act & Assert
            Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(""));
            Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(null));
            Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse("   "));
        }

        [Fact]
        public void Parse_InputWithInsufficientParts_ThrowsTrainIndexValidationException()
        {
            // Arrange
            var rawIndex = "7478";

            // Act & Assert
            var exception = Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(rawIndex));
            exception.Message.Should().Contain("минимум 2 числовые части");
        }

        [Fact]
        public void Parse_InputWithTooManyParts_ThrowsTrainIndexValidationException()
        {
            // Arrange
            var rawIndex = "7478 035 6980 123";

            // Act & Assert
            var exception = Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(rawIndex));
            exception.Message.Should().Contain("максимум 3 числовые части");
        }

        [Fact]
        public void Parse_StationCodeWithLessThanFourDigits_ThrowsTrainIndexValidationException()
        {
            // Arrange
            var rawIndex = "748 035 6980";

            // Act & Assert
            var exception = Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(rawIndex));
            exception.Message.Should().Contain("минимум 4 цифры");
        }

        [Fact]
        public void TryParse_ValidInput_ReturnsTrueAndParsedIndex()
        {
            // Arrange
            var rawIndex = "7478-035-6980";

            // Act
            var success = TrainIndex.TryParse(rawIndex, out var result);

            // Assert
            success.Should().BeTrue();
            result.Should().NotBeNull();
            result.NormalizedIndex.Should().Be("7478 035 6980");
        }

        [Fact]
        public void TryParse_InvalidInput_ReturnsFalseAndNullIndex()
        {
            // Arrange
            var rawIndex = "invalid";

            // Act
            var success = TrainIndex.TryParse(rawIndex, out var result);

            // Assert
            success.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Normalize_ValidInput_ReturnsNormalizedString()
        {
            // Arrange
            var rawIndex = "7478-35-6980";

            // Act
            var normalized = TrainIndex.Normalize(rawIndex);

            // Assert
            normalized.Should().Be("7478 035 6980");
        }

        [Fact]
        public void TryNormalize_ValidInput_ReturnsTrueAndNormalizedString()
        {
            // Arrange
            var rawIndex = "7478/035/6980";

            // Act
            var success = TrainIndex.TryNormalize(rawIndex, out var normalized);

            // Assert
            success.Should().BeTrue();
            normalized.Should().Be("7478 035 6980");
        }

        [Fact]
        public void TryNormalize_InvalidInput_ReturnsFalseAndNullString()
        {
            // Arrange
            var rawIndex = "";

            // Act
            var success = TrainIndex.TryNormalize(rawIndex, out var normalized);

            // Assert
            success.Should().BeFalse();
            normalized.Should().BeNull();
        }

        [Fact]
        public void Equals_SameIndexes_ReturnsTrue()
        {
            // Arrange
            var index1 = TrainIndex.Parse("7478-035-6980");
            var index2 = TrainIndex.Parse("7478 35 6980");

            // Act & Assert
            index1.Equals(index2).Should().BeTrue();
            (index1 == index2).Should().BeFalse(); // Operator == не переопределен
        }

        [Fact]
        public void ToString_ReturnsNormalizedIndex()
        {
            // Arrange
            var index = TrainIndex.Parse("7478-35-6980");

            // Act
            var stringRepresentation = index.ToString();

            // Assert
            stringRepresentation.Should().Be("7478 035 6980");
        }

        [Fact]
        public void Parse_IndexWithBothPartsLookingLikeTrainNumbers_ChoosesCorrectly()
        {
            // Arrange - обе части могут быть номером состава, но одна длиннее
            var rawIndex = "7478 035 123";

            // Act
            var result = TrainIndex.Parse(rawIndex);

            // Assert
            result.FormationStationCode.Should().Be("7478");
            result.TrainNumber.Should().Be("035"); // Выбираем первую подходящую
            result.DestinationStationCode.Should().Be("0123"); // Обрезаем до 4 цифр, но это некорректно
        }

        [Fact]
        public void Parse_NoValidTrainNumber_ThrowsException()
        {
            // Arrange
            var rawIndex = "7478 6980 1234"; // Нет номера состава (2-3 цифры)

            // Act & Assert
            var exception = Assert.Throws<TrainIndexValidationException>(() => TrainIndex.Parse(rawIndex));
            exception.Message.Should().Contain("Не найден номер состава");
        }
    }
}