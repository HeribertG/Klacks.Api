using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Assistant;

public class PostCommandValidator : AbstractValidator<PostCommand<LLMModel>>
{
    private readonly ILLMRepository _repository;

    public PostCommandValidator(ILLMRepository repository)
    {
        _repository = repository;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Resource.ModelId)
            .NotEmpty()
            .WithMessage("ModelId is required")
            .MaximumLength(50)
            .WithMessage("ModelId must not exceed 50 characters")
            .Matches("^[a-zA-Z0-9-_.]+$")
            .WithMessage("ModelId can only contain alphanumeric characters, hyphens, underscores, and dots")
            .MustAsync(async (modelId, cancellation) => 
            {
                var existing = await _repository.GetModelByIdAsync(modelId);
                return existing == null;
            })
            .WithMessage("A model with this ModelId already exists");

        RuleFor(x => x.Resource.ModelName)
            .NotEmpty()
            .WithMessage("ModelName is required")
            .MaximumLength(100)
            .WithMessage("ModelName must not exceed 100 characters");

        RuleFor(x => x.Resource.ProviderId)
            .NotEmpty()
            .WithMessage("ProviderId is required")
            .MaximumLength(50)
            .WithMessage("ProviderId must not exceed 50 characters")
            .MustAsync(async (providerId, cancellation) => 
            {
                var provider = await _repository.GetProviderByIdAsync(providerId);
                return provider != null;
            })
            .WithMessage("The specified provider does not exist");

        RuleFor(x => x.Resource.ContextWindow)
            .GreaterThan(0)
            .WithMessage("ContextWindow must be greater than 0")
            .LessThanOrEqualTo(2000000)
            .WithMessage("ContextWindow must not exceed 2,000,000 tokens");

        RuleFor(x => x.Resource.MaxTokens)
            .GreaterThan(0)
            .WithMessage("MaxTokens must be greater than 0")
            .LessThanOrEqualTo(x => x.Resource.ContextWindow)
            .WithMessage("MaxTokens must not exceed ContextWindow");

        RuleFor(x => x.Resource.CostPerInputToken)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CostPerInputToken must be non-negative")
            .PrecisionScale(10, 6, true)
            .WithMessage("CostPerInputToken must have at most 6 decimal places");

        RuleFor(x => x.Resource.CostPerOutputToken)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CostPerOutputToken must be non-negative")
            .PrecisionScale(10, 6, true)
            .WithMessage("CostPerOutputToken must have at most 6 decimal places");

        RuleFor(x => x.Resource.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Resource.Category)
            .MaximumLength(50)
            .WithMessage("Category must not exceed 50 characters");
    }

}