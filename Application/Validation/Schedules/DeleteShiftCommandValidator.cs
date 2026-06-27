// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Validates that a shift can be deleted — blocked when active work entries still reference it.
 * @param id - The shift ID to delete
 */

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Schedules;

public class DeleteShiftCommandValidator : AbstractValidator<DeleteCommand<ShiftResource>>
{
    public DeleteShiftCommandValidator(IShiftRepository shiftRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => !await shiftRepository.HasActiveWorksAsync(id, ct))
            .WithMessage("shift.validation.has-active-works");
    }
}
