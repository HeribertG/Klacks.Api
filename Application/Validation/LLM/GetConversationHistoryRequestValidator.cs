using FluentValidation;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Application.Validation.LLM;

public class GetConversationHistoryRequestValidator : AbstractValidator<GetConversationHistoryRequest>
{
    public GetConversationHistoryRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Limit must not exceed 100");

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Offset must be non-negative");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(450)
            .WithMessage("UserId must not exceed 450 characters");
    }
}