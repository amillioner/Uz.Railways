using FluentValidation;
using Rail.Data.Models;

namespace Rail.Services.Validators
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
}