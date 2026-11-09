// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Validates that a client (employee) can be removed from a group — blocked when planned works exist
 * for that client on any shift in the group. The check runs only for a group the caller can see, so a hidden group is
 * answered by the handler like a missing link instead of a validation error that would reveal planned works in it.
 * @param clientId - The client to remove
 * @param groupId - The group to remove the client from
 * @param groupVisibilityGuard - Decides whether the caller can see the group
 */

using FluentValidation;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Associations;

public class RemoveGroupItemByClientAndGroupCommandValidator : AbstractValidator<RemoveGroupItemByClientAndGroupCommand>
{
    public RemoveGroupItemByClientAndGroupCommandValidator(
        IShiftRepository shiftRepository, IGroupVisibilityGuard groupVisibilityGuard)
    {
        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
                !await shiftRepository.HasWorksForClientInGroupAsync(command.ClientId, command.GroupId, null, ct))
            .WithMessage("group.validation.member-has-planned-works")
            .WhenAsync((command, ct) => groupVisibilityGuard.IsGroupVisibleAsync(command.GroupId, ct));
    }
}
