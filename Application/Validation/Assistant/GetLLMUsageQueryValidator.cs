using FluentValidation;
using Klacks.Api.Application.Queries.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class GetLLMUsageQueryValidator : AbstractValidator<GetLLMUsageQuery>
{
    public GetLLMUsageQueryValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(450)
            .WithMessage("UserId must not exceed 450 characters");

        RuleFor(x => x.Days)
            .GreaterThan(0)
            .WithMessage("Days must be greater than 0")
            .LessThanOrEqualTo(365)
            .WithMessage("Days must not exceed 365");
    }
}