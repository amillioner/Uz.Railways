using FluentValidation;
using Rail.Api.Models;

namespace Rail.Api.Validators
{
    /// <summary>
    /// Validator for train filter parameters
    /// </summary>
    public class TrainFilterValidator : AbstractValidator<TrainFilterDto>
    {
        public TrainFilterValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.SearchIndex)
                .MaximumLength(50)
                .WithMessage("Search index must not exceed 50 characters");

            RuleFor(x => x.CreatedAfter)
                .LessThan(x => x.CreatedBefore)
                .When(x => x.CreatedBefore.HasValue)
                .WithMessage("CreatedAfter must be before CreatedBefore");

            RuleFor(x => x.MinWagons)
                .GreaterThanOrEqualTo(0)
                .LessThan(x => x.MaxWagons)
                .When(x => x.MaxWagons.HasValue)
                .WithMessage("MinWagons must be less than MaxWagons");

            RuleFor(x => x.MaxWagons)
                .GreaterThan(0)
                .WithMessage("MaxWagons must be greater than 0");

            RuleFor(x => x.MinWeight)
                .GreaterThanOrEqualTo(0)
                .LessThan(x => x.MaxWeight)
                .When(x => x.MaxWeight.HasValue)
                .WithMessage("MinWeight must be less than MaxWeight");

            RuleFor(x => x.MaxWeight)
                .GreaterThan(0)
                .WithMessage("MaxWeight must be greater than 0");

            RuleFor(x => x.SortBy)
                .Must(BeValidSortField)
                .WithMessage("SortBy must be one of: Id, NormalizedIndex, CreatedAt, WagonsCount, TotalWeight");

            RuleFor(x => x.SortDirection)
                .Must(x => x.ToLower() == "asc" || x.ToLower() == "desc")
                .WithMessage("SortDirection must be 'asc' or 'desc'");
        }

        private bool BeValidSortField(string sortBy)
        {
            var validFields = new[] { "Id", "NormalizedIndex", "CreatedAt", "WagonsCount", "TotalWeight" };
            return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Validator for CSV file uploads
    /// </summary>
    public class CsvFileValidator : AbstractValidator<IFormFile>
    {
        private static readonly string[] AllowedExtensions = { ".csv", ".txt" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public CsvFileValidator()
        {
            RuleFor(x => x)
                .NotNull()
                .WithMessage("File is required");

            RuleFor(x => x.Length)
                .GreaterThan(0)
                .WithMessage("File cannot be empty")
                .LessThanOrEqualTo(MaxFileSize)
                .WithMessage($"File size cannot exceed {MaxFileSize / (1024 * 1024)}MB");

            RuleFor(x => x.FileName)
                .Must(HaveValidExtension)
                .WithMessage($"File must have one of the following extensions: {string.Join(", ", AllowedExtensions)}");

            RuleFor(x => x.ContentType)
                .Must(BeValidContentType)
                .WithMessage("File must be a CSV or text file");
        }

        private bool HaveValidExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        private bool BeValidContentType(string contentType)
        {
            var validContentTypes = new[]
            {
                "text/csv",
                "text/plain",
                "application/csv",
                "application/vnd.ms-excel"
            };

            return validContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
        }
    }
}