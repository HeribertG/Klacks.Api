// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class GetConversationHistoryRequestValidator : AbstractValidator<GetConversationHistoryRequest>
{
    public GetConversationHistoryRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(LLMValidationLimits.MaxConversationHistoryLimit)
            .WithMessage($"Limit must not exceed {LLMValidationLimits.MaxConversationHistoryLimit}");

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Offset must be non-negative");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(LLMValidationLimits.MaxUserIdLength)
            .WithMessage($"UserId must not exceed {LLMValidationLimits.MaxUserIdLength} characters");
    }
}