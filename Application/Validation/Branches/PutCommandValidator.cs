using FluentValidation;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Validation.Branches;

public class PutCommandValidator : AbstractValidator<PutCommand>
{
    public PutCommandValidator(IBranchRepository branchRepository)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.model.Name)
            .NotEmpty().WithMessage("Name is required")
            .MustAsync(async (command, name, cancellation) =>
                !await branchRepository.ExistsByNameAsync(name, command.model.Id))
            .WithMessage("A branch with this name already exists.");
    }
}
