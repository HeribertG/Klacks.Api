// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Constants;
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
            .MaximumLength(LLMValidationLimits.MaxUserIdLength)
            .WithMessage($"UserId must not exceed {LLMValidationLimits.MaxUserIdLength} characters");

        RuleFor(x => x.Days)
            .GreaterThan(0)
            .WithMessage("Days must be greater than 0")
            .LessThanOrEqualTo(LLMValidationLimits.MaxUsageDays)
            .WithMessage($"Days must not exceed {LLMValidationLimits.MaxUsageDays}");
    }
}