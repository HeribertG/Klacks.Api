using FluentValidation;
using Klacks.Api.Application.Commands.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required for deletion");
    }
}