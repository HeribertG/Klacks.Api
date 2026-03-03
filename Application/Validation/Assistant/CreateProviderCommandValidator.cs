// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
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
            .MaximumLength(50)
            .WithMessage("ProviderId must not exceed 50 characters")
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
            .MaximumLength(100)
            .WithMessage("ProviderName must not exceed 100 characters");

        RuleFor(x => x.ApiKey)
            .MinimumLength(10)
            .WithMessage("ApiKey must be at least 10 characters long")
            .When(x => !string.IsNullOrEmpty(x.ApiKey));

        RuleFor(x => x.BaseUrl)
            .Must(BeAValidUrl)
            .WithMessage("BaseUrl must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.BaseUrl));

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 100)
            .WithMessage("Priority must be between 1 and 100");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) 
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}