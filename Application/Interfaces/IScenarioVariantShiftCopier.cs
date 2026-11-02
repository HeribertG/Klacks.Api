// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Copies existing real order families into a variant (key). Stage-only: the copies are only staged, the caller commits
/// them through IUnitOfWork. Every refusal is thrown before anything is staged, so a refused call leaves nothing behind.
/// Originals are never changed, and satellites that carry no token (expenses, required qualifications) are copied by the
/// new shift id; client shift preferences are not copied.
/// </summary>
/// <param name="StageCopyOfShiftsAsync">Copies the families of the given shifts, whichever member of a family is named, once per family and clipped to the variant period; refuses a shift that is missing, not a real sealed order, without a customer, outside the variant period, not reducible to it or with a group outside the variant hierarchy</param>
/// <param name="StageCopyOfGroupAsync">Copies every visible sealed family of the source group and its subgroups that fits the variant, and counts open orders and families that do not fit as skipped</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScenarioVariantShiftCopier
{
    Task<CopyShiftsToVariantResult> StageCopyOfShiftsAsync(
        AnalyseScenario variant, IReadOnlyCollection<Guid> variantGroupIds, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task<CopyShiftsToVariantResult> StageCopyOfGroupAsync(
        AnalyseScenario variant, IReadOnlyCollection<Guid> variantGroupIds, Guid sourceGroupId, CancellationToken cancellationToken = default);
}
