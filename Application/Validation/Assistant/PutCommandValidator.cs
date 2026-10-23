// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Commands;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class PutCommandValidator : AbstractValidator<PutCommand<LLMModel>>
{
    public PutCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Resource.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Id cannot be empty");

        RuleFor(x => x.Resource.ModelId)
            .NotEmpty()
            .WithMessage("ModelId is required")
            .MaximumLength(LLMValidationLimits.MaxModelIdLength)
            .WithMessage($"ModelId must not exceed {LLMValidationLimits.MaxModelIdLength} characters")
            .Matches("^[a-zA-Z0-9-_.]+$")
            .WithMessage("ModelId can only contain alphanumeric characters, hyphens, underscores, and dots");

        RuleFor(x => x.Resource.ModelName)
            .NotEmpty()
            .WithMessage("ModelName is required")
            .MaximumLength(LLMValidationLimits.MaxModelNameLength)
            .WithMessage($"ModelName must not exceed {LLMValidationLimits.MaxModelNameLength} characters");

        RuleFor(x => x.Resource.ProviderId)
            .NotEmpty()
            .WithMessage("ProviderId is required")
            .MaximumLength(LLMValidationLimits.MaxProviderIdLength)
            .WithMessage($"ProviderId must not exceed {LLMValidationLimits.MaxProviderIdLength} characters");

        RuleFor(x => x.Resource.ContextWindow)
            .GreaterThan(0)
            .WithMessage("ContextWindow must be greater than 0")
            .LessThanOrEqualTo(LLMValidationLimits.MaxContextWindowTokens)
            .WithMessage("ContextWindow must not exceed 2,000,000 tokens");

        RuleFor(x => x.Resource.MaxTokens)
            .GreaterThan(0)
            .WithMessage("MaxTokens must be greater than 0")
            .LessThanOrEqualTo(x => x.Resource.ContextWindow)
            .WithMessage("MaxTokens must not exceed ContextWindow");

        RuleFor(x => x.Resource.CostPerInputToken)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CostPerInputToken must be non-negative")
            .PrecisionScale(LLMValidationLimits.TokenCostPrecision, LLMValidationLimits.TokenCostScale, true)
            .WithMessage($"CostPerInputToken must have at most {LLMValidationLimits.TokenCostScale} decimal places");

        RuleFor(x => x.Resource.CostPerOutputToken)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CostPerOutputToken must be non-negative")
            .PrecisionScale(LLMValidationLimits.TokenCostPrecision, LLMValidationLimits.TokenCostScale, true)
            .WithMessage($"CostPerOutputToken must have at most {LLMValidationLimits.TokenCostScale} decimal places");

        RuleFor(x => x.Resource.Description)
            .MaximumLength(LLMValidationLimits.MaxModelDescriptionLength)
            .WithMessage($"Description must not exceed {LLMValidationLimits.MaxModelDescriptionLength} characters");

        RuleFor(x => x.Resource.Category)
            .MaximumLength(LLMValidationLimits.MaxModelCategoryLength)
            .WithMessage($"Category must not exceed {LLMValidationLimits.MaxModelCategoryLength} characters");
    }

}