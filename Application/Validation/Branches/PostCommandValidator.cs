// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;

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
