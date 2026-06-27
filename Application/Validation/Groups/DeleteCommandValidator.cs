// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Validates that a group can be deleted — blocked when planned works exist for any shift
 * in the group or any of its descendant groups (the entire subtree is checked).
 */

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Groups;

public class DeleteCommandValidator : AbstractValidator<DeleteCommand<GroupResource>>
{
    public DeleteCommandValidator(IShiftRepository shiftRepository, IGroupHierarchyService groupHierarchyService)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                IEnumerable<Guid> groupIds;
                try
                {
                    var subtree = await groupHierarchyService.GetDescendantsAsync(id, includeParent: true);
                    groupIds = subtree.Select(g => g.Id);
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }

                return !await shiftRepository.HasWorksForAnyGroupAsync(groupIds, null, ct);
            })
            .WithMessage("group.validation.has-planned-works");
    }
}
