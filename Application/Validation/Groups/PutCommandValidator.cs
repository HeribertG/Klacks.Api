// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Validates update commands for groups: name, date ranges, duplicate members,
 * and guards against setting ValidUntil while planned works exist after that date
 * or removing a member who still has planned works in the group.
 */

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Validation.Groups;

public class PutCommandValidator : AbstractValidator<PutCommand<GroupResource>>
{
    public PutCommandValidator(IShiftRepository shiftRepository, IGroupRepository groupRepository)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
        RuleFor(x => x.Resource).Must(x => x.ValidFrom.Ticks != 0).WithMessage("ValidFrom: Valid date is required");

        RuleFor(x => x.Resource)
            .Must(x => !x.ValidUntil.HasValue || x.ValidUntil.Value > x.ValidFrom)
            .WithMessage("group.validation.valid-until-must-be-after-valid-from");

        RuleFor(x => x.Resource.GroupItems).Must(x =>
        {
            var list = x.Select(x => x.ClientId).Distinct().ToList();
            return x.Count == list.Count;
        }).When(x => x.Resource.GroupItems.Any()).WithMessage("The list of participants must not contain any duplicates");

        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
            {
                if (!command.Resource.ValidUntil.HasValue) return true;
                var afterDate = DateOnly.FromDateTime(command.Resource.ValidUntil.Value);
                return !await shiftRepository.HasWorksForGroupAsync(command.Resource.Id, afterDate, ct);
            })
            .WithMessage("group.validation.works-exist-after-valid-until");

        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
            {
                var incomingClientIds = command.Resource.GroupItems
                    .Where(gi => gi.ClientId.HasValue)
                    .Select(gi => gi.ClientId!.Value)
                    .ToHashSet();

                Group? currentGroup;
                try
                {
                    currentGroup = await groupRepository.Get(command.Resource.Id);
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }

                if (currentGroup == null) return true;

                foreach (var item in currentGroup.GroupItems.Where(gi => gi.ClientId.HasValue))
                {
                    if (!incomingClientIds.Contains(item.ClientId!.Value))
                    {
                        if (await shiftRepository.HasWorksForClientInGroupAsync(item.ClientId!.Value, command.Resource.Id, null, ct))
                            return false;
                    }
                }
                return true;
            })
            .WithMessage("group.validation.member-has-planned-works");
    }
}
