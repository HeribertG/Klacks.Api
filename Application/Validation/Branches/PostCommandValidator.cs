using FluentValidation;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Validation.Branches;

public class PostCommandValidator : AbstractValidator<PostCommand>
{
    public PostCommandValidator(IBranchRepository branchRepository)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.model.Name)
            .NotEmpty().WithMessage("Name is required")
            .MustAsync(async (name, cancellation) =>
                !await branchRepository.ExistsByNameAsync(name))
            .WithMessage("A branch with this name already exists.");
    }
}
