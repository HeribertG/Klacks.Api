// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftCutFacade
{
    Task<List<Shift>> ProcessBatchCutsAsync(List<CutOperation> operations);

    Task<List<Shift>> ResetCutsAsync(Guid originalId, DateOnly newStartDate);
}
