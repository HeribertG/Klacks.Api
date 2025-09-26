using FluentValidation;
using Klacks.Api.Application.Commands.LLM;

namespace Klacks.Api.Application.Validation.LLM;

public class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required for deletion");
    }
}