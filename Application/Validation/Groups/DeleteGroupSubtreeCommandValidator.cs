// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Validates that a group subtree can be deleted — blocked when planned works exist for any shift in the
 * group or any of its descendant groups, exactly like the single group delete. Deleting a group removes the
 * memberships of its groups, so planned works must be cleaned up first.
 */

using FluentValidation;
using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Groups;

public class DeleteGroupSubtreeCommandValidator : AbstractValidator<DeleteGroupSubtreeCommand>
{
    public DeleteGroupSubtreeCommandValidator(IShiftRepository shiftRepository, IGroupHierarchyService groupHierarchyService)
    {
        RuleFor(x => x.Id)
            .MustAsync((id, ct) => GroupSubtreePlannedWorksRule.HasNoPlannedWorksAsync(id, shiftRepository, groupHierarchyService, ct))
            .WithMessage(GroupSubtreePlannedWorksRule.ErrorMessageKey);
    }
}
