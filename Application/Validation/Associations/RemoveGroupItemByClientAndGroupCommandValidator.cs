// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Validates that a client (employee) can be removed from a group — blocked when planned works exist
 * for that client on any shift in the group.
 * @param clientId - The client to remove
 * @param groupId - The group to remove the client from
 */

using FluentValidation;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Associations;

public class RemoveGroupItemByClientAndGroupCommandValidator : AbstractValidator<RemoveGroupItemByClientAndGroupCommand>
{
    public RemoveGroupItemByClientAndGroupCommandValidator(IShiftRepository shiftRepository)
    {
        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
                !await shiftRepository.HasWorksForClientInGroupAsync(command.ClientId, command.GroupId, null, ct))
            .WithMessage("group.validation.member-has-planned-works");
    }
}
