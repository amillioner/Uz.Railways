using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Rail.Services.Validators;

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