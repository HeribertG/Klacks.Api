// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Keeps WorkChanges off container works: a container counts its whole span, so it is corrected through its inner
/// services, and a partial cover runs through a split. An inner service of a container takes no replacement either.
/// Corrections and replacements are refused separately, so either rule can be lifted on its own.
/// </summary>
/// <param name="shiftRepository">Reads the shift of the work to tell a container</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerWorkChangeGuard : IContainerWorkChangeGuard
{
    private const string CorrectionMessage =
        "A container cannot be corrected itself; correct its inner services instead.";
    private const string ReplacementMessage =
        "A container cannot be replaced itself; split it to hand part of it to another person.";

    private readonly IShiftRepository _shiftRepository;

    public ContainerWorkChangeGuard(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task EnsureNoCorrectionAsync(WorkChange workChange, Work parentWork)
    {
        if (CorrectionWorkChangeTypes.IsCorrection(workChange.Type) && await IsContainerAsync(parentWork))
        {
            throw new ContainerWorkChangeRefusedException(CorrectionMessage, ContainerWorkChangeRefusalCodes.CorrectionRefused);
        }
    }

    public async Task EnsureNoReplacementAsync(WorkChange workChange, Work parentWork)
    {
        if (ReplacementWorkChangeTypes.IsReplacement(workChange.Type)
            && (parentWork.ParentWorkId != null || await IsContainerAsync(parentWork)))
        {
            throw new ContainerWorkChangeRefusedException(ReplacementMessage, ContainerWorkChangeRefusalCodes.ReplacementRefused);
        }
    }

    private async Task<bool> IsContainerAsync(Work work)
    {
        var shift = await _shiftRepository.Get(work.ShiftId);
        return shift != null && ContainerShiftRules.IsContainer(shift);
    }
}
