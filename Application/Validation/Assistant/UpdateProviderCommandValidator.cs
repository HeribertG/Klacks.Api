using FluentValidation;
using Klacks.Api.Application.Commands.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

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