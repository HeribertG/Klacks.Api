// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Validates that a group-item can be deleted.
 * Blocks deletion if the item is a client (employee) who has planned works in the group,
 * or a shift that has active works.
 * @param id - The GroupItem ID to delete
 */

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Groups;

public class GroupItemDeleteCommandValidator : AbstractValidator<DeleteCommand<GroupItemResource>>
{
    public GroupItemDeleteCommandValidator(IGroupItemRepository groupItemRepository, IShiftRepository shiftRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var item = await groupItemRepository.Get(id);
                if (item == null) return true;

                if (item.ClientId.HasValue)
                    return !await shiftRepository.HasWorksForClientInGroupAsync(item.ClientId.Value, item.GroupId, null, ct);

                if (item.ShiftId.HasValue)
                    return !await shiftRepository.HasActiveWorksAsync(item.ShiftId.Value, ct);

                return true;
            })
            .WithMessage("group.validation.group-item-has-planned-works");
    }
}
