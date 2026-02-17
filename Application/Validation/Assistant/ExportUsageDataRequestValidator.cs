using FluentValidation;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class ExportUsageDataRequestValidator : AbstractValidator<ExportUsageDataRequest>
{
    private readonly string[] _validFormats = { "csv", "json", "xlsx" };

    public ExportUsageDataRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("Format is required")
            .Must(BeValidFormat)
            .WithMessage($"Format must be one of: {string.Join(", ", _validFormats)}");

        RuleFor(x => x.Days)
            .GreaterThan(0)
            .WithMessage("Days must be greater than 0")
            .LessThanOrEqualTo(365)
            .WithMessage("Days must not exceed 365");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(450)
            .WithMessage("UserId must not exceed 450 characters");
    }

    private bool BeValidFormat(string format)
    {
        return _validFormats.Contains(format?.ToLower());
    }
}