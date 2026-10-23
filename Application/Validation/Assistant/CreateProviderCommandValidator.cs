// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Assistant;

public class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    private readonly ILLMRepository _repository;

    public CreateProviderCommandValidator(ILLMRepository repository)
    {
        _repository = repository;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("ProviderId is required")
            .MaximumLength(LLMValidationLimits.MaxProviderIdLength)
            .WithMessage($"ProviderId must not exceed {LLMValidationLimits.MaxProviderIdLength} characters")
            .Matches("^[a-zA-Z0-9-_]+$")
            .WithMessage("ProviderId can only contain alphanumeric characters, hyphens, and underscores")
            .MustAsync(async (providerId, cancellation) => 
            {
                var existing = await _repository.GetProviderByIdAsync(providerId);
                return existing == null;
            })
            .WithMessage("A provider with this ProviderId already exists");

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .WithMessage("ProviderName is required")
            .MaximumLength(LLMValidationLimits.MaxProviderNameLength)
            .WithMessage($"ProviderName must not exceed {LLMValidationLimits.MaxProviderNameLength} characters");

        RuleFor(x => x.ApiKey)
            .MinimumLength(LLMValidationLimits.MinApiKeyLength)
            .WithMessage($"ApiKey must be at least {LLMValidationLimits.MinApiKeyLength} characters long")
            .When(x => !string.IsNullOrEmpty(x.ApiKey));

        RuleFor(x => x.BaseUrl)
            .Must(BeAValidUrl)
            .WithMessage("BaseUrl must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.BaseUrl));

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, LLMValidationLimits.MaxProviderPriority)
            .WithMessage($"Priority must be between 1 and {LLMValidationLimits.MaxProviderPriority}");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) 
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}